// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_reporting_handled_events_to_the_observer : given.a_performing_job_step
{
    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_counts_broken_down_by_event_type_not_event_payloads() => _observer.Received(3).ReportHandledEvents(
        Arg.Any<Key>(),
        Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(counts => counts.Values.Sum(count => (long)count.Value) == 1));
}
