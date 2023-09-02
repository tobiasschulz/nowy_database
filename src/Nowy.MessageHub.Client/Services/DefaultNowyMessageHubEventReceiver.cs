using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nowy.Database.Contract.Services;

namespace Nowy.MessageHub.Client.Services;

internal class DefaultNowyMessageHubEventReceiver : INowyMessageHubReceiver
{
    public IEnumerable<string> GetEventNamePrefixes()
    {
        throw new NotImplementedException();
    }

    public Task ReceiveMessageAsync(string event_name, INowyMessageHubReceiverPayload payload)
    {
        throw new NotImplementedException();
    }
}
