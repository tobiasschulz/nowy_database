using Nowy.Database.Contract.Services;

namespace Nowy.Database.Contract.Models;

public interface IMessageHubPeer
{
    string[] Names { get; }
    NowyMessageHubPeerType Type { get; }
}
