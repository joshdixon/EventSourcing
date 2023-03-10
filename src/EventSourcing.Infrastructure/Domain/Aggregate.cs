namespace EventSourcing.Infrastructure.Domain;

public abstract class Aggregate : Aggregate<Guid>, IAggregate
{
}

public abstract class Aggregate<T> : IAggregate<T> where T : notnull
{
    public T Id { get; protected set; } = default!;

    public int Version { get; protected set; }

    [NonSerialized] private readonly Queue<object> uncommittedEvents = new Queue<object>();

    public virtual void When(object @event)
    {
    }

    public IEnumerable<object> DequeueUncommittedEvents()
    {
        var dequeuedEvents = uncommittedEvents.ToArray();

        uncommittedEvents.Clear();

        return dequeuedEvents;
    }

    protected void Enqueue(object @event)
    {
        uncommittedEvents.Enqueue(@event);
    }
}