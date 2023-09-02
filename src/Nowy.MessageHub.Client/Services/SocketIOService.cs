using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nowy.Database.Contract.Services;
using SocketIOClient;

namespace Nowy.MessageHub.Client.Services;

internal sealed class SocketIOService : BackgroundService
{
    private static readonly JsonSerializerOptions _json_options = new JsonSerializerOptions() { PropertyNamingPolicy = null, };
    private static readonly string RAW_EVENT_NAME_BROADCAST_MESSAGE = "v1:broadcast_message";

    private readonly ILogger _logger;
    private readonly SocketIOConfig _config;
    private readonly List<INowyMessageHubReceiver> _receivers_from_di;
    private ImmutableList<INowyMessageHubReceiver> _receivers_ephemeral;
    private readonly List<EndpointEntry> _clients;

    public SocketIOService(ILogger<SocketIOService> logger, SocketIOConfig config, IEnumerable<INowyMessageHubReceiver> receivers)
    {
        this._logger = logger;
        this._config = config;
        this._receivers_from_di = receivers.ToList();
        this._receivers_ephemeral = ImmutableList<INowyMessageHubReceiver>.Empty;

        List<EndpointEntry> clients = new();
        foreach (NowyMessageHubEndpointConfig endpoint_config in config.Endpoints)
        {
            this._logger.LogInformation("Use MessageHub Endpoint: {messagehub_url}", endpoint_config.Url);

            SocketIOClient.SocketIO client = new(endpoint_config.Url, new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = int.MaxValue,
                ReconnectionDelay = 5000,
                RandomizationFactor = 0.5,
                ConnectionTimeout = TimeSpan.FromSeconds(5),

                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("token", "abc123"),
                    new KeyValuePair<string, string>("key", "value")
                }
            });

            // client.OnAny((a, b) => { _logger.LogInformation($"Received message from Socket IO: event_name={a}, b={JsonSerializer.Serialize(b)}"); });

            client.On(RAW_EVENT_NAME_BROADCAST_MESSAGE, this._handleBroadcastResponseAsync);

            clients.Add(new EndpointEntry(client, endpoint_config));
        }

        this._clients = clients;
    }

    public void AddEphemeralReceiver(INowyMessageHubReceiver receiver)
    {
        this._receivers_ephemeral = this._receivers_ephemeral.Add(receiver);
    }

    public void RemoveEphemeralReceiver(INowyMessageHubReceiver receiver)
    {
        this._receivers_ephemeral = this._receivers_ephemeral.RemoveAll(r => r == receiver);
    }

    private async void _handleBroadcastResponseAsync(SocketIOResponse response)
    {
        string event_name = response.GetValue<string>(0);
        MessageOptions message_options = response.GetValue<MessageOptions>(1);
        int values_count = response.GetValue<int>(2);

        this._logger.LogInformation("Received message from Socket IO: {event_name} with {values_count} values", event_name, values_count);

        async ValueTask handle_async(INowyMessageHubReceiver receiver)
        {
            bool matches = false;
            foreach (string event_name_prefix in receiver.GetEventNamePrefixes())
            {
                if (event_name.StartsWith(event_name_prefix, StringComparison.Ordinal))
                {
                    matches = true;
                    break;
                }
            }

            if (!matches)
                return;

            List<string> values_as_json = new();

            for (int i = 0; i < values_count; i++)
            {
                string value_as_json = response.GetValue<string>(3 + i);
                values_as_json.Add(value_as_json);
            }

            this._logger.LogInformation("Handle message from Socket IO: {event_name}: {data}", event_name, values_as_json);

            _Payload payload = new(values_as_json, _json_options);

            await receiver.ReceiveMessageAsync(event_name, payload);
        }

        foreach (INowyMessageHubReceiver receiver in this._receivers_from_di)
        {
            await handle_async(receiver);
        }

        foreach (INowyMessageHubReceiver receiver in this._receivers_ephemeral)
        {
            await handle_async(receiver);
        }
    }

    public async Task WaitUntilConnectedAsync(string event_name, TimeSpan delay)
    {
        using CancellationTokenSource cts = new();
        cts.CancelAfter(delay);
        await this.WaitUntilConnectedAsync(event_name, cts.Token);
    }

    public async Task WaitUntilConnectedAsync(string event_name, CancellationToken token)
    {
        List<EndpointEntry> matching_endpoint_entries = new();
        foreach (EndpointEntry client_entry in this._clients)
        {
            if (client_entry.EndpointConfig.IsEventNameAllowed(event_name))
            {
                matching_endpoint_entries.Add(client_entry);
            }
        }

        while (!token.IsCancellationRequested)
        {
            bool is_connected = matching_endpoint_entries.Any(o => o.Client.Connected);
            if (is_connected)
            {
                break;
            }

            await Task.Delay(100);
        }
    }

    private bool _isConnected(string event_name)
    {
        bool? is_connected = null;

        foreach (EndpointEntry client_entry in this._clients)
        {
            if (client_entry.EndpointConfig.IsEventNameAllowed(event_name))
            {
                if (client_entry.Client.Connected)
                {
                    is_connected = true;
                }
            }
        }

        return is_connected ?? false;
    }

    public async Task BroadcastMessageAsync(string event_name, object[] values, NowyMessageOptions? message_options)
    {
        List<object> data = new();
        data.Add(event_name);

        MessageOptions socket_message_options = new()
        {
            except_sender = message_options?.ExceptSender ?? false,
        };
        data.Add(socket_message_options);

        data.Add(values.Length);
        foreach (object o in values)
        {
            data.Add(o as string ?? JsonSerializer.Serialize(o, _json_options));
        }

        this._logger.LogInformation("Send message to Socket IO: {data}", data);

        await Task.WhenAll(this._clients.Select(async client_entry =>
        {
            NowyMessageHubEndpointConfig endpoint_config = client_entry.EndpointConfig;
            if (endpoint_config.IsEventNameAllowed(event_name))
            {
                SocketIOClient.SocketIO client = client_entry.Client;
                if (!client.Connected)
                    throw new InvalidOperationException($"Endpoint '{client_entry.EndpointConfig.Url}' is not connected.");

                await client.EmitAsync(eventName: RAW_EVENT_NAME_BROADCAST_MESSAGE, data: data.ToArray());
            }
        }));
    }

    internal sealed class _Payload : INowyMessageHubReceiverPayload
    {
        private readonly List<string> _values_as_json;
        private readonly JsonSerializerOptions _json_options;

        private int _next_index = 0;

        public _Payload(List<string> values_as_json, JsonSerializerOptions json_options)
        {
            this._values_as_json = values_as_json;
            this._json_options = json_options;
        }

        public int Count => this._values_as_json.Count;

        public TValue? GetValue<TValue>() where TValue : class
        {
            int index = this._next_index;
            TValue? value = this.GetValue<TValue>(index);
            this._next_index++;
            return value;
        }

        public TValue? GetValue<TValue>(int index) where TValue : class
        {
            string str = this._values_as_json[index];

            if (typeof(TValue) == typeof(string))
            {
                return str as TValue;
            }

            return JsonSerializer.Deserialize<TValue>(str, this._json_options);
        }
    }

    internal sealed class EndpointEntry
    {
        internal readonly SocketIOClient.SocketIO Client;
        internal readonly NowyMessageHubEndpointConfig EndpointConfig;

        public EndpointEntry(SocketIOClient.SocketIO client, NowyMessageHubEndpointConfig endpoint_config)
        {
            this.Client = client;
            this.EndpointConfig = endpoint_config;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await Task.WhenAll(this._clients.Select(async client_entry =>
        {
            await Task.Yield();

            SocketIOClient.SocketIO client = client_entry.Client;

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!client.Connected)
                    {
                        try
                        {
                            this._logger.LogInformation("Connect to Socket IO: {url}", client_entry.EndpointConfig.Url);
                            await client.ConnectAsync();
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogError(ex, $"Connect to Socket IO: Error during {nameof(client.ConnectAsync)}");

                            await Task.Delay(this._config.ConnectionRetryDelay, stoppingToken);
                        }
                    }

                    await Task.Delay(this._config.ConnectionRetryLoopDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Connect to Socket IO: Error during {nameof(this.ExecuteAsync)}");
            }
        }).ToArray());
    }


    internal class MessageOptions
    {
        [JsonPropertyName("except_sender")] public bool except_sender { get; set; }
    }
}
