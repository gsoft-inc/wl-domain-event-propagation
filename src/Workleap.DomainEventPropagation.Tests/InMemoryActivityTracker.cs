using System.Diagnostics;

namespace Workleap.DomainEventPropagation.Tests;

internal sealed class InMemoryActivityTracker : IDisposable
{
    private static readonly HashSet<InMemoryActivityTracker> ActiveTrackers = new HashSet<InMemoryActivityTracker>();

    private readonly List<Activity> _activities;

    static InMemoryActivityTracker()
    {
        // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-collection-walkthroughs?source=recommendations#add-code-to-collect-the-traces
        var staticListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Workleap.DomainEventPropagation",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (ActiveTrackers)
                {
                    foreach (var tracker in ActiveTrackers)
                    {
                        tracker.TrackActivity(activity);
                    }
                }
            },
        };

        ActivitySource.AddActivityListener(staticListener);
    }

    public InMemoryActivityTracker()
    {
        this._activities = new List<Activity>();

        lock (ActiveTrackers)
        {
            ActiveTrackers.Add(this);
        }
    }

    private void TrackActivity(Activity activity)
    {
        lock (this._activities)
        {
            this._activities.Add(activity);
        }
    }

    public void AssertPublishSuccessful()
    {
        lock (this._activities)
        {
            var activity = Assert.Single(this._activities);

            Assert.Equal("EventGridEvents create", activity.OperationName);
            Assert.Equal(ActivityKind.Producer, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            Assert.Equal("OK", activity.GetTagItem(TracingHelper.StatusCodeTag));
        }
    }

    public void AssertPublishFailed(string requestName, Exception exception)
    {
        lock (this._activities)
        {
            var activity = Assert.Single(this._activities);

            Assert.Equal("EventGridEvents create", activity.OperationName);
            Assert.Equal(requestName, activity.DisplayName);
            Assert.Equal(ActivityKind.Internal, activity.Kind);
            Assert.Equal(ActivityStatusCode.Error, activity.Status);

            Assert.Equal("ERROR", activity.GetTagItem(TracingHelper.StatusCodeTag));
            Assert.Equal(exception.Message, activity.GetTagItem(TracingHelper.StatusDescriptionTag));
            Assert.Equal(exception.Message, activity.GetTagItem(TracingHelper.ExceptionMessageTag));
            Assert.Equal(exception.GetType().FullName!, activity.GetTagItem(TracingHelper.ExceptionTypeTag));

            var stacktrace = Assert.IsType<string>(activity.GetTagItem(TracingHelper.ExceptionStackTraceTag));
            Assert.NotEmpty(stacktrace);
        }
    }

    public void AssertSubscribeSuccessful()
    {
        lock (this._activities)
        {
            var activity = Assert.Single(this._activities);

            Assert.Equal("EventGridEvents process", activity.OperationName);
            Assert.Equal(ActivityKind.Consumer, activity.Kind);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            Assert.Equal("OK", activity.GetTagItem(TracingHelper.StatusCodeTag));
        }
    }

    public void Dispose()
    {
        lock (ActiveTrackers)
        {
            ActiveTrackers.Remove(this);
        }
    }
}