namespace EventSourcing.Infrastructure.Domain;

public interface IAggregateRepository<T> where T : IAggregate
{
    Task<T?> Find(Guid id, CancellationToken cancelToken = default);

    Task Add(T aggregate, CancellationToken cancelToken = default);

    Task Update(T aggregate, CancellationToken cancelToken = default);
}