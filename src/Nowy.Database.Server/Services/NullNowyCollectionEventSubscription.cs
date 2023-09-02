using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;

namespace Nowy.Database.Server.Services;

internal class NullNowyCollectionEventSubscription<TModel> : INowyCollectionEventSubscription<TModel> where TModel : class, IBaseModel
{
    public void Dispose()
    {
    }

    public INowyCollectionEventSubscription<TModel> AddHandler<TEvent>(Action<TEvent> handler) where TEvent : CollectionEvent
    {
        return this;
    }

    INowyCollectionEventSubscription INowyCollectionEventSubscription.AddHandler<TEvent>(Action<TEvent> handler)
    {
        return this.AddHandler<TEvent>(handler);
    }

    public INowyCollectionEventSubscription<TModel> AddHandler<TEvent>(Func<TEvent, ValueTask> handler) where TEvent : CollectionEvent
    {
        return this;
    }

    INowyCollectionEventSubscription INowyCollectionEventSubscription.AddHandler<TEvent>(Func<TEvent, ValueTask> handler)
    {
        return this.AddHandler<TEvent>(handler);
    }
}
