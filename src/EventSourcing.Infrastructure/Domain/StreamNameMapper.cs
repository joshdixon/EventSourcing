using System.Collections.Concurrent;

namespace EventSourcing.Infrastructure.Domain;

public class StreamNameMapper
{
    private static readonly ConcurrentDictionary<Type, string> _typeNameMap = new();

    public static void AddCustomMap<TStream>(string mappedStreamName) =>
        AddCustomMap(typeof(TStream), mappedStreamName);

    public static void AddCustomMap(Type streamType, string mappedStreamName)
    {
        _typeNameMap.AddOrUpdate(streamType, mappedStreamName, (_, _) => mappedStreamName);
    }

    public static string ToStreamPrefix<TStream>() => ToStreamPrefix(typeof(TStream));

    public static string ToStreamPrefix(Type streamType) =>
        _typeNameMap.GetOrAdd(streamType, type => type.FullName!.Replace(".", "_"));

    public static string ToStreamId<TStream>(object aggregateId, object? tenantId = null) =>
        ToStreamId(typeof(TStream), aggregateId);

    // Generates a stream id in the canonical `{category}-{aggregateId}` format
    public static string ToStreamId(Type streamType, object aggregateId, object? tenantId = null)
    {
        var tenantPrefix = tenantId == null ? $"{tenantId}_" : "";
        var category = ToStreamPrefix(streamType);

        // (Out-of-the box, the category projection treats anything before a `-` separator as the category name)
        // For this reason, we place the "{tenantId}_" bit (if present) on the right hand side of the '-'
        return $"{category}-{tenantPrefix}{aggregateId}";
    }
}