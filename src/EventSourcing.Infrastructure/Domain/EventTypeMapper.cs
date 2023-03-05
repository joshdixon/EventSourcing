using System.Collections.Concurrent;

namespace EventSourcing.Infrastructure.Domain;

internal static class EventTypeMapper
{
    private static readonly ConcurrentDictionary<Type, string> _typeNameMap = new();
    private static readonly ConcurrentDictionary<string, Type?> _typeMap = new();

    public static void AddCustomMap<T>(string mappedEventTypeName) => AddCustomMap(typeof(T), mappedEventTypeName);

    public static void AddCustomMap(Type eventType, string mappedEventTypeName)
    {
        _typeNameMap.AddOrUpdate(eventType, mappedEventTypeName, (_, _) => mappedEventTypeName);
        _typeMap.AddOrUpdate(mappedEventTypeName, eventType, (_, _) => eventType);
    }

    public static string ToName<TEventType>() => ToName(typeof(TEventType));

    public static string ToName(Type eventType) => _typeNameMap.GetOrAdd(eventType, type =>
    {
        var eventTypeName = type.FullName!.Replace(".", "_");

        _typeMap.AddOrUpdate(eventTypeName, type, (_, _) => type);

        return eventTypeName;
    });

    public static Type? ToType(string eventTypeName) => _typeMap.GetOrAdd(eventTypeName, key =>
    {
        var typeName = key.Replace("_", ".");
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes().Where(x => x.FullName == typeName || x.Name == typeName))
            .FirstOrDefault();
        if (type == null)
            return null;

        _typeNameMap.AddOrUpdate(type, key, (_, _) => key);

        return type;
    });
}