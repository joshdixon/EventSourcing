using System.Diagnostics.Metrics;
using System.Reflection;

namespace EventSourcing.Infrastructure;

internal class ServiceMeters
{
    internal static readonly Meter Meter = new Meter(Assembly.GetEntryAssembly()?.GetName()?.Name ?? throw new Exception("Could not get entry assembly name"));
}
