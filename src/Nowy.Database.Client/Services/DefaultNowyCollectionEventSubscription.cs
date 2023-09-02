using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;

namespace Nowy.Database.Client.Services;

public class DefaultNowyCollectionEventSubscription<TModel> : INowyCollectionEventSubscription<TModel> where TModel : class, IBaseModel
{
    private readonly string _database_name;
    private readonly string _entity_name;

    private INowyCollection<TModel>? _collection;
    private ILogger? _logger;
    private INowyMessageHub? _message_hub;
    private ImmutableArray<(Type event_type, Func<CollectionEvent, ValueTask> action)> _handlers = ImmutableArray<(Type event_type, Func<CollectionEvent, ValueTask> action)>.Empty;

    public DefaultNowyCollectionEventSubscription(INowyCollection<TModel> collection, ILogger logger, INowyMessageHub message_hub)
    {
        this._database_name = collection.DatabaseName;
        this._entity_name = collection.EntityName;

        this._collection = collection;
        this._logger = logger;
        this._message_hub = message_hub;
    }

    private void _ensureSubscription<TEvent>() where TEvent : CollectionEvent
    {
        if (this._message_hub is null) throw new ArgumentNullException(nameof(this._message_hub));

        _message_hub.SubscribeEvent<TEvent>(config =>
        {
            config.Where(e => e.DatabaseName == this._database_name && e.EntityName == this._entity_name);
            config.AddHandler(_handleReceivedEvent);
        });
    }

    private async ValueTask _handleReceivedEvent(CollectionEvent event_value)
    {
        Type event_type = event_value.GetType();
        foreach (( Type event_type, Func<CollectionEvent, ValueTask> action ) handler in this._handlers)
        {
            if (event_type == handler.event_type)
            {
                try
                {
                    await handler.action(event_value);
                }
                catch (Exception ex)
                {
                    this._logger?.LogError(ex, "Error during database collection event handler: {event}", event_value);
                }
            }
        }
    }

    public INowyCollectionEventSubscription<TModel> AddHandler<TEvent>(Action<TEvent> handler) where TEvent : CollectionEvent
    {
        _ensureSubscription<TEvent>();
        this._handlers = this._handlers.Add(( typeof(TEvent), e =>
        {
            handler((TEvent)e);
            return ValueTask.CompletedTask;
        } ));
        return this;
    }

    INowyCollectionEventSubscription INowyCollectionEventSubscription.AddHandler<TEvent>(Action<TEvent> handler)
    {
        return this.AddHandler<TEvent>(handler);
    }

    public INowyCollectionEventSubscription<TModel> AddHandler<TEvent>(Func<TEvent, ValueTask> handler) where TEvent : CollectionEvent
    {
        _ensureSubscription<TEvent>();
        this._handlers = this._handlers.Add(( typeof(TEvent), e => handler((TEvent)e) ));
        return this;
    }

    INowyCollectionEventSubscription INowyCollectionEventSubscription.AddHandler<TEvent>(Func<TEvent, ValueTask> handler)
    {
        return this.AddHandler<TEvent>(handler);
    }

    public void Dispose()
    {
        this._collection = null;
        this._message_hub = null;
        if (!this._handlers.IsDefaultOrEmpty)
        {
            this._handlers = this._handlers.Clear();
        }
    }
}
