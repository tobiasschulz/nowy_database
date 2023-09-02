namespace Nowy.MessageHub.Client.Services;

internal sealed class SocketIOConfig
{
    public List<NowyMessageHubEndpointConfig> Endpoints { get; set; } = new();
    public TimeSpan ConnectionRetryDelay { get; set; } = TimeSpan.FromMilliseconds(5000);
    public TimeSpan ConnectionRetryLoopDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
