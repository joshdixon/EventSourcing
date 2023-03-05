using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing.Infrastructure;

public static class ActivitySourceExtensions
{

    public static RootActivity StartRootActivity(this ActivitySource source,
        string name,
        ActivityKind kind = ActivityKind.Internal,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        var parent = Activity.Current;
        Activity.Current = null;
        var next = source.StartActivity(name, kind,
            parentContext: default,
            tags: tags,
            links: new[] { new ActivityLink(parent.Context) });

        return new RootActivity(next, parent);
    }

}

public class RootActivity : IDisposable
{
    public Activity Activity { get; }
    public Activity ParentActivity { get; }

    public RootActivity(Activity activity, Activity parentActivity)
    {
        Activity = activity;
        ParentActivity = parentActivity;
    }

    private bool _disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Activity.Dispose();
                Activity.Current = ParentActivity;
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
