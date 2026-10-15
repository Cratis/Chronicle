// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing.with_observer_filters;

/// <summary>
/// Historical delivery applies the same filters as live delivery: only the tagged events reach the subscriber,
/// and the trailing excluded event is read past without being claimed as handled.
/// </summary>
public class and_some_events_are_excluded : given.a_performing_job_step_with_observer_filters
{
    void Establish() => _eventCursor.Current.Returns([CreateEvent(1UL, _audited), CreateEvent(2UL), CreateEvent(3UL, _audited), CreateEvent(4UL)]);

    Task Because() => PerformStep();

    [Fact] void should_deliver_only_the_events_that_pass_the_filters() => _handledBatches.Single().ShouldEqual([1UL, 3UL]);
    [Fact] void should_report_the_last_delivered_event_as_handled() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_report_having_read_up_to_the_last_event() => _result.LastScannedEventSequenceNumber.ShouldEqual((EventSequenceNumber)4UL);
}
