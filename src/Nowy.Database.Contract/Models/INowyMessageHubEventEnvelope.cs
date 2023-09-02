namespace Nowy.Database.Contract.Models;

public interface INowyMessageHubEventEnvelopeBuilder
{
    void AddRecipient(string name);
    void AddValue(object value);
}

public interface INowyMessageHubEventEnvelope
{
    IMessageHubPeer Sender { get; }
    object[] Values { get; }
}

public static class NowyMessageHubEventEnvelopeBuilderExtensions
{
    public static void AddRecipient(this INowyMessageHubEventEnvelopeBuilder that, IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            that.AddRecipient(name: name);
        }
    }

    public static void AddRecipients(this INowyMessageHubEventEnvelopeBuilder that, params string[] names)
    {
        foreach (string name in names)
        {
            that.AddRecipient(name: name);
        }
    }

    public static void AddValue(this INowyMessageHubEventEnvelopeBuilder that, IEnumerable<string> values)
    {
        foreach (string value in values)
        {
            that.AddValue(value: value);
        }
    }

    public static void AddValues(this INowyMessageHubEventEnvelopeBuilder that, params string[] values)
    {
        foreach (string value in values)
        {
            that.AddValue(value: value);
        }
    }
}
