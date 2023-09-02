using Nowy.Database.Contract.Models;

namespace Nowy.MessageHub.Client.Services;

internal class DefaultNowyMessageHubEventEnvelopeBuilder : INowyMessageHubEventEnvelopeBuilder
{
    private readonly List<string> _recipients = new();
    private readonly List<object> _values = new();
    private TimeSpan? _send_delay;

    public void AddRecipient(string name)
    {
        this._recipients.Add(name);
    }

    public void AddValue(object value)
    {
        this._values.Add(value);
    }

    public void SetSendDelay(TimeSpan? value)
    {
        _send_delay = value;
    }

    public IReadOnlyList<string> Recipients => _recipients;

    public IReadOnlyList<object> Values => _values;

    public TimeSpan? SendDelay => _send_delay;
}
