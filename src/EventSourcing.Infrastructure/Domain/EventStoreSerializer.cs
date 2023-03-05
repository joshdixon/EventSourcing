using System.Text;
using System.Text.Json;
using EventStore.Client;

namespace EventSourcing.Infrastructure.Domain;

internal static class EventStoreSerializer
{
    public static T? Deserialize<T>(this ResolvedEvent resolvedEvent) where T : class =>
        Deserialize(resolvedEvent) as T;

    public static object? Deserialize(this ResolvedEvent resolvedEvent)
    {
        var eventType = EventTypeMapper.ToType(resolvedEvent.Event.EventType);

        var data = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);

        return eventType != null
            ? JsonSerializer.Deserialize(data, eventType)
            : null;
    }

    public static async Task<EventData> ToJsonEventData(this object @event)
    {
        using var dataStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(dataStream, @event);
        var dataBytes = dataStream.ToArray();

        using var metadataStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(metadataStream, new { });
        var metadataBytes = metadataStream.ToArray();

        return new EventData(
            Uuid.NewUuid(),
            EventTypeMapper.ToName(@event.GetType()),
            dataBytes,
            metadataBytes
        );
    }
}