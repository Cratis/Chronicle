// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_dispatching;

/// <summary>
/// Dispatching two separate appends must not inherit the activity that started the queue.
/// </summary>
public class and_two_appends_have_separate_trace_contexts : Specification
{
    readonly ConcurrentQueue<Activity> _activities = new();
    readonly ActivitySource _source = new("separate-dispatch-contexts-specification");
    readonly EventType _eventType = new("event", 1);
    AppendedEventsQueue _queue;
    ActivityListener _listener;

    async Task Establish()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, _source),
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "cratis.chronicle.appended_events_queue.dispatch")
                {
                    _activities.Enqueue(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(_listener);
        var grainFactory = Substitute.For<IGrainFactory>();
        var observerKey = new ObserverKey("observer", "store", "namespace", "sequence");
        grainFactory.GetGrain<IObserver>(observerKey).Returns(Substitute.For<IObserver>());
        using var caller = new Activity("queue-caller").Start();
        _queue = new(
            new Cratis.Tasks.TaskFactory(),
            grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            new Cratis.Traces.ActivitySource<AppendedEventsQueue>(_source),
            Options.Create(new ChronicleOptions()),
            NullLogger<AppendedEventsQueue>.Instance);
        await _queue.Subscribe(observerKey, [_eventType]);
    }

    async Task Because()
    {
        await Append("first-append");
        await Append("second-append");
    }

    [Fact] void should_trace_both_dispatches() => _activities.Count.ShouldEqual(2);
    [Fact] void should_not_inherit_a_parent() => _activities.All(activity => activity.ParentSpanId == default).ShouldBeTrue();
    [Fact] void should_use_independent_traces() => _activities.Select(activity => activity.TraceId).Distinct().Count().ShouldEqual(2);

    async Task Append(string operation)
    {
        using var append = new Activity(operation).Start();
        var @event = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with { EventType = _eventType, EventSourceId = "source" }
        };
        await _queue.Enqueue([@event]);
        await _queue.AwaitQueueDepletion().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    void Destroy()
    {
        _queue.Dispose();
        _listener.Dispose();
        _source.Dispose();
    }
}
