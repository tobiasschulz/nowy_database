using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nowy.Database.Contract.Services;

namespace Nowy.MessageHub.Client.Services;

public readonly record struct EventQueueKey(string Name, NowyMessageOptions MessageOptions);

public readonly record struct EventQueueEntry(object Value, TimeSpan SendDelay, Stopwatch AddedAt);

internal class DefaultNowyMessageHubEventQueue : BackgroundService
{
    private static readonly TimeSpan DefaultSendDelay = TimeSpan.FromMilliseconds(1000);

    private readonly ILogger<DefaultNowyMessageHubEventQueue> _logger;
    private readonly DefaultNowyMessageHubInternal _message_hub_internal;
    private readonly object _lock_entries = new();
    private ImmutableDictionary<EventQueueKey, ImmutableHashSet<EventQueueEntry>> _entries = ImmutableDictionary<EventQueueKey, ImmutableHashSet<EventQueueEntry>>.Empty;
    private bool _entries_exist = false;

    public DefaultNowyMessageHubEventQueue(ILogger<DefaultNowyMessageHubEventQueue> logger, DefaultNowyMessageHubInternal message_hub_internal)
    {
        _logger = logger;
        _message_hub_internal = message_hub_internal;
    }

    public void QueueBroadcastMessage(string event_name, object event_value, NowyMessageOptions message_options)
    {
        EventQueueKey key = new(
            Name: event_name,
            MessageOptions: message_options
        );
        EventQueueEntry entry = new(
            Value: event_value,
            SendDelay: message_options.SendDelay ?? DefaultSendDelay,
            AddedAt: Stopwatch.StartNew()
        );
        lock (this._lock_entries)
        {
            if (this._entries.TryGetValue(key, out ImmutableHashSet<EventQueueEntry>? list))
            {
                list = list.Add(entry);
                this._entries = this._entries.SetItem(key, list);
            }
            else
            {
                list = ImmutableHashSet<EventQueueEntry>.Empty.Add(entry);
                this._entries = this._entries.SetItem(key, list);
            }

            this._entries_exist = true;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await Task.Run(async () =>
        {
            const int delay_milliseconds = 1000;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (this._entries_exist)
                    {
                        ImmutableDictionary<EventQueueKey, ImmutableHashSet<EventQueueEntry>> entries_copied = this._entries;
                        foreach (( EventQueueKey key, ImmutableHashSet<EventQueueEntry> entries_with_key ) in entries_copied)
                        {
                            foreach (EventQueueEntry entry in entries_with_key)
                            {
                                if (entry.AddedAt.Elapsed >= entry.SendDelay)
                                {
                                    await this._message_hub_internal.BroadcastMessageAsync(
                                        event_name: key.Name,
                                        values: entries_with_key.Select(o => o.Value).ToArray(),
                                        options: key.MessageOptions
                                    );

                                    lock (this._lock_entries)
                                    {
                                        this._entries.TryGetValue(key, out ImmutableHashSet<EventQueueEntry>? entries_with_key2);
                                        entries_with_key2 ??= ImmutableHashSet<EventQueueEntry>.Empty;
                                        entries_with_key2 = entries_with_key2.Remove(entry);
                                        this._entries = this._entries.SetItem(key, entries_with_key2);
                                    }
                                }
                            }
                        }

                        this._entries_exist = this._entries.Values.Any(o => o.Count != 0);

                        TimeSpan? delay_shortest = null;
                        entries_copied = this._entries;
                        foreach (( EventQueueKey key, ImmutableHashSet<EventQueueEntry> entries_with_key ) in entries_copied)
                        {
                            foreach (EventQueueEntry entry in entries_with_key)
                            {
                                if (entry.AddedAt.Elapsed < entry.SendDelay)
                                {
                                    TimeSpan delay_for_entry = entry.SendDelay - entry.AddedAt.Elapsed;
                                    if (delay_shortest > delay_for_entry)
                                    {
                                        delay_shortest = delay_for_entry;
                                    }
                                }
                            }
                        }

                        if (delay_shortest is { } d)
                        {
                            await Task.Delay(d, stoppingToken);
                        }
                    }
                    else
                    {
                        await Task.Delay(delay_milliseconds, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
            }
        });
    }
}
