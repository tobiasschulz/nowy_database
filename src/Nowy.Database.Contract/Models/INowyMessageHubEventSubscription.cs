using System.Collections.Immutable;

namespace Nowy.Database.Contract.Models;

public interface INowyMessageHubEventSubscriptionBuilder<out TEvent> where TEvent : class
{
    INowyMessageHubEventSubscriptionBuilder<TEvent> Where(Predicate<TEvent> value);
    INowyMessageHubEventSubscriptionBuilder<TEvent> AddHandler(Action<TEvent> value);
    INowyMessageHubEventSubscriptionBuilder<TEvent> AddHandler(Func<TEvent, ValueTask> value);
}

public interface INowyMessageHubEventSubscription : IDisposable
{
}

public static class NowyMessageHubEventSubscriptionBuilderExtensions
{
}

public sealed class NowyImmutableMessageHubEventSubscriptionCollection : IDisposable
{
    private ImmutableArray<INowyMessageHubEventSubscription> Subscriptions = ImmutableArray<INowyMessageHubEventSubscription>.Empty;

    public void Dispose()
    {
        foreach (INowyMessageHubEventSubscription subscription in Subscriptions)
        {
            subscription.Dispose();
        }

        Subscriptions = ImmutableArray<INowyMessageHubEventSubscription>.Empty;
    }

    public void Add(INowyMessageHubEventSubscription value)
    {
        Subscriptions = Subscriptions.Add(value);
    }

    public void AddRange(params INowyMessageHubEventSubscription[] values)
    {
        Subscriptions = Subscriptions.AddRange(values);
    }
}
