namespace Nowy.Database.Contract.Services;

public interface INowyMessageHubReceiver
{
    IEnumerable<string> GetEventNamePrefixes();

    Task ReceiveMessageAsync(string event_name, INowyMessageHubReceiverPayload payload);
}
