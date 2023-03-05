namespace EventSourcing.Infrastructure.Domain;

public interface IAggregate : IAggregate<Guid>
{
}

public interface IAggregate<out T> : IProjection
{
    T Id { get; }
    int Version { get; }

    IEnumerable<object> DequeueUncommittedEvents();
}