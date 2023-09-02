using System.Text.Json.Serialization;

namespace Nowy.Database.Contract.Models;

public abstract class CollectionEvent : IEquatable<CollectionEvent>
{
    [JsonPropertyName("database_name")] public string? DatabaseName { get; set; }
    [JsonPropertyName("entity_name")] public string? EntityName { get; set; }

    public bool Equals(CollectionEvent? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return this.DatabaseName == other.DatabaseName && this.EntityName == other.EntityName;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CollectionEvent)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.DatabaseName, this.EntityName);
    }
}

public abstract class CollectionModelEvent : CollectionEvent, IEquatable<CollectionModelEvent>
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    public bool Equals(CollectionModelEvent? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && this.Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CollectionModelEvent)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), this.Id);
    }
}

public sealed class CollectionChangedEvent : CollectionEvent
{
}

public sealed class CollectionModelInsertedEvent : CollectionModelEvent
{
}

public sealed class CollectionModelUpdatedEvent : CollectionModelEvent
{
}

public sealed class CollectionModelDeletedEvent : CollectionModelEvent
{
}

public sealed class CollectionModelsInsertedEvent : CollectionEvent
{
}

public sealed class CollectionModelsUpdatedEvent : CollectionEvent
{
}

public sealed class CollectionModelsDeletedEvent : CollectionEvent
{
}
