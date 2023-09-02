using Nowy.Database.Contract.Services;

namespace Nowy.MessageHub.Client.Services;

internal class DefaultNowyMessageHubInternal
{
    private readonly SocketIOService _socket_io_service;

    public DefaultNowyMessageHubInternal(SocketIOService socket_io_service)
    {
        this._socket_io_service = socket_io_service;
    }

    public async Task BroadcastMessageAsync(string event_name, object[] values, NowyMessageOptions? options)
    {
        await this._socket_io_service.BroadcastMessageAsync(event_name, values, options);
    }

    public async Task WaitUntilConnectedAsync(string event_name, CancellationToken token)
    {
        await this._socket_io_service.WaitUntilConnectedAsync(event_name, token);
    }

    public async Task WaitUntilConnectedAsync(string event_name, TimeSpan delay)
    {
        await this._socket_io_service.WaitUntilConnectedAsync(event_name, delay);
    }

    public void AddEphemeralReceiver(INowyMessageHubReceiver receiver)
    {
        this._socket_io_service.AddEphemeralReceiver(receiver);
    }

    public void RemoveEphemeralReceiver(INowyMessageHubReceiver receiver)
    {
        this._socket_io_service.RemoveEphemeralReceiver(receiver);
    }
}
