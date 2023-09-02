namespace Nowy.Database.Contract.Services;

public interface INowyMessageHubReceiverPayload
{
    int Count { get; }
    TValue? GetValue<TValue>() where TValue : class;
    TValue? GetValue<TValue>(int index) where TValue : class;
}
