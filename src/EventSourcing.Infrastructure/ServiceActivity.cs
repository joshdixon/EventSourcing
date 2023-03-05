using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing.Infrastructure;

internal static class ServiceActivitySource
{
    private static readonly AssemblyName _assemblyName = Assembly.GetEntryAssembly()?.GetName() ?? throw new Exception("Could not get entry assembly name");

    internal static readonly ActivitySource ActivitySource = new ActivitySource(_assemblyName.Name, _assemblyName.Version.ToString());
}

public class ServiceActivity : IDisposable
{
    private readonly IDisposable? _activity;
    private readonly IDisposable? _tagScope;

    public ServiceActivity(ILogger logger,
      string className,
      [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
      params (string Key, object? Value)[] customTags)
    {
        _activity = ServiceActivitySource.ActivitySource.StartActivity($"{className}.{memberName}");

        Dictionary<string, object?> tags = new Dictionary<string, object?>()
        {
            { "Class", className },{ "Method", memberName }
        };

        if (customTags?.Any() == true)
        {
            foreach ((string Key, object? Value) tag in customTags)
            {
                tags.Add(tag.Key, tag.Value);
            }
        }

        _tagScope = logger.BeginScope(tags);
    }

    public ServiceActivity(ILogger logger,
      string className,
      [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
      bool isRoot = false,
      params (string Key, object? Value)[] customTags)
    {
        _activity = ServiceActivitySource.ActivitySource.StartRootActivity($"{className}.{memberName}");

        Dictionary<string, object?> tags = new Dictionary<string, object?>()
        {
            { "Class", className },{ "Method", memberName }
        };

        if (customTags?.Any() == true)
        {
            foreach ((string Key, object? Value) tag in customTags)
            {
                tags.Add(tag.Key, tag.Value);
            }
        }

        _tagScope = logger.BeginScope(tags);
    }

    public void Dispose()
    {
        _activity?.Dispose();
        _tagScope?.Dispose();
    }
}
