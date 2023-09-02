using Microsoft.Extensions.Logging;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;

namespace Nowy.MessageHub.Client.Services;

internal class DefaultNowyMessageHubEventSubscriptionBuilder<TEvent> : INowyMessageHubEventSubscriptionBuilder<TEvent> where TEvent : class
{
    private readonly ILogger _logger;
    private readonly DefaultNowyMessageHubInternal _message_hub_internal;
    private readonly string[] _event_names;

    private readonly List<Predicate<TEvent>> _predicates = new();
    private readonly List<Func<TEvent, ValueTask>> _handlers = new();

    public DefaultNowyMessageHubEventSubscriptionBuilder(
        ILogger logger,
        DefaultNowyMessageHubInternal message_hub_internal,
        string event_name
    )
    {
        _logger = logger;
        _message_hub_internal = message_hub_internal;
        _event_names = new[] { event_name, };
    }

    public INowyMessageHubEventSubscriptionBuilder<TEvent> Where(Predicate<TEvent> value)
    {
        _predicates.Add(value);
        return this;
    }

    public INowyMessageHubEventSubscriptionBuilder<TEvent> AddHandler(Action<TEvent> value)
    {
        _handlers.Add(e =>
        {
            value(e);
            return ValueTask.CompletedTask;
        });
        return this;
    }

    public INowyMessageHubEventSubscriptionBuilder<TEvent> AddHandler(Func<TEvent, ValueTask> value)
    {
        _handlers.Add(value);
        return this;
    }

    public DefaultNowyMessageHubEventSubscription<TEvent> Build()
    {
        return new DefaultNowyMessageHubEventSubscription<TEvent>(
            this._logger,
            this._message_hub_internal,
            this._event_names,
            this._predicates,
            this._handlers
        );
    }
}

internal class DefaultNowyMessageHubEventSubscription<TEvent> : INowyMessageHubReceiver, INowyMessageHubEventSubscription where TEvent : class
{
    private readonly ILogger _logger;
    private readonly DefaultNowyMessageHubInternal _message_hub_internal;
    private readonly string[] _event_names;
    private readonly IReadOnlyList<Predicate<TEvent>> _predicates;
    private readonly IReadOnlyList<Func<TEvent, ValueTask>> _handlers;

    public DefaultNowyMessageHubEventSubscription(
        ILogger logger,
        DefaultNowyMessageHubInternal message_hub_internal,
        string[] event_names,
        IReadOnlyList<Predicate<TEvent>> predicates,
        IReadOnlyList<Func<TEvent, ValueTask>> handlers
    )
    {
        _logger = logger;
        _message_hub_internal = message_hub_internal;
        _event_names = event_names;
        _predicates = predicates;
        _handlers = handlers;

        _message_hub_internal.AddEphemeralReceiver(this);
    }

    public void Dispose()
    {
        _message_hub_internal.RemoveEphemeralReceiver(this);
    }

    public IEnumerable<string> GetEventNamePrefixes()
    {
        return this._event_names;
    }

    public async Task ReceiveMessageAsync(string event_name, INowyMessageHubReceiverPayload payload)
    {
        this._logger.LogInformation("Received event in ephemeral receiver: {event_name}, {payload}", event_name, payload);

        if (this._event_names.Contains(event_name))
        {
            for (int i = 0; i < payload.Count; i++)
            {
                TEvent? message = payload.GetValue<TEvent>(i);
                if (message is null) throw new ArgumentNullException(nameof(payload.GetValue));

                if (this._predicates.All(pred => pred(message)))
                {
                    foreach (Func<TEvent, ValueTask> handler in this._handlers)
                    {
                        try
                        {
                            await handler(message);
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogInformation(ex, "Error during handeling received event in ephemeral receiver: {event_name}, {payload}", event_name, payload);
                        }
                    }
                }
            }
        }
    }
}
