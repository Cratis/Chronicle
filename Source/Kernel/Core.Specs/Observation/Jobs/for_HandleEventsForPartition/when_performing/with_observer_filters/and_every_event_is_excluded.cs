// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing.with_observer_filters;

/// <summary>
/// The partition holds only events the observer's filters exclude. None of them may reach the subscriber, yet the
/// step has read them, and says so without claiming any of them as handled.
/// </summary>
public class and_every_event_is_excluded : given.a_performing_job_step_with_observer_filters
{
    void Establish() => _eventCursor.Current.Returns([CreateEvent(1UL), CreateEvent(2UL, [new("other")])]);

    Task Because() => PerformStep();

    [Fact] void should_not_deliver_any_events() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_succeed() => _succeeded.ShouldBeTrue();
    [Fact] void should_not_report_any_event_as_handled() => _result.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_report_having_read_up_to_the_last_event() => _result.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
}
