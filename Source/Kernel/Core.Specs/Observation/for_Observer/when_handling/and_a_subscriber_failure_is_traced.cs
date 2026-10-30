// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>A subscriber exception marks the observer's handle span.</summary>
public class and_a_subscriber_failure_is_traced : given.an_observer_with_subscription_for_specific_event_type
{
    ActivityListener _listener;
    ActivityTraceId _trace;
    Activity _handle;

    void Establish()
    {
        _listener = new()
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == _trace && activity.OperationName == "cratis.chronicle.observer.handle") _handle = activity;
            }
        };
        ActivitySource.AddActivityListener(_listener);
        _subscriber.When(_ => _.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())).Throws(new InvalidOperationException("private message"));
    }

    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        await _observer.Handle("source", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);
    }

    [Fact] void should_mark_the_span_as_failed() => _handle.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_the_exception_type() => _handle.GetTagItem("error.type").ShouldEqual(typeof(InvalidOperationException).FullName);
    [Fact] void should_not_record_the_message() => string.IsNullOrEmpty(_handle.StatusDescription).ShouldBeTrue();

    void Destroy() => _listener.Dispose();
}
