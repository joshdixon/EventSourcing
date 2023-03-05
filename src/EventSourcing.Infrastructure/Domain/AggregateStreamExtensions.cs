using EventStore.Client;

namespace EventSourcing.Infrastructure.Domain;

public static class AggregateStreamExtensions
{
    public static async Task<T?> AggregateStream<T>(
        this EventStoreClient eventStore,
        Guid id,
        ulong? fromVersion = null,
        CancellationToken cancelToken = default
    ) where T : class, IProjection
    {
        EventStoreClient.ReadStreamResult readResult = eventStore.ReadStreamAsync(
            Direction.Forwards,
            StreamNameMapper.ToStreamId<T>(id),
            fromVersion ?? StreamPosition.Start,
            cancellationToken: cancelToken
        );

        ReadState readState = await readResult.ReadState;

        if (readState == ReadState.StreamNotFound)
            return null;

        return await readResult.AggregateAsync(
            (T)Activator.CreateInstance(typeof(T), true)!,
            (aggregate, @event) =>
            {
                object? eventData = @event.Deserialize();
                aggregate.When(eventData!);
                return aggregate;
            },
            cancelToken);
    }
}