using EventStore.Client;

namespace EventSourcing.Infrastructure.Domain;

public class EventStoreAggregateRepository<T> : IAggregateRepository<T> where T : class, IAggregate
{
    private readonly EventStoreClient _eventStore;

    public EventStoreAggregateRepository(EventStoreClient eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<T?> Find(Guid id, CancellationToken cancelToken = default) =>
        await _eventStore.AggregateStream<T>(
            id,
            cancelToken: cancelToken
        );

    public async Task Add(T aggregate, CancellationToken cancelToken = default) => await Store(aggregate, cancelToken);

    public async Task Update(T aggregate, CancellationToken cancelToken = default) =>
        await Store(aggregate, cancelToken);

    private async Task Store(T aggregate, CancellationToken cancelToken)
    {
        var events = aggregate.DequeueUncommittedEvents();

        var eventsToStore = await Task.WhenAll(events
            .Select(EventStoreSerializer.ToJsonEventData));

        await _eventStore.AppendToStreamAsync(
            StreamNameMapper.ToStreamId<T>(aggregate.Id),
            // TODO: Add proper optimistic concurrency handling
            StreamState.Any,
            eventsToStore,
            cancellationToken: cancelToken
        );
    }
}