using Nowy.Database.Contract.Models;

namespace Nowy.Database.Contract.Services;

public interface INowyCollectionEventSubscription<TModel> : INowyCollectionEventSubscription where TModel : class, IBaseModel
{
    INowyCollectionEventSubscription<TModel> AddHandler<TEvent>(Action<TEvent> handler) where TEvent : CollectionEvent;
    INowyCollectionEventSubscription<TModel> AddHandler<TEvent>(Func<TEvent, ValueTask> handler) where TEvent : CollectionEvent;
}

public interface INowyCollectionEventSubscription : IDisposable
{
    INowyCollectionEventSubscription AddHandler<TEvent>(Action<TEvent> handler) where TEvent : CollectionEvent;
    INowyCollectionEventSubscription AddHandler<TEvent>(Func<TEvent, ValueTask> handler) where TEvent : CollectionEvent;
}
