// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// A reducer subscriber reports a failed batch as having handled none of its events, because nothing of the batch was
/// written to the read model. Catch-up and retry must then record the failure at the batch's first event so the next
/// attempt folds the whole batch again (Cratis/Chronicle#4540).
/// </summary>
public class and_the_subscriber_fails_without_handling_any_events : given.a_performing_job_step
{
    void Establish() =>
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "reducer failed")));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact]
    void should_report_partition_failed_at_the_first_event_of_the_batch() =>
        _observer.Received(1).PartitionFailed(
            Arg.Any<Key>(),
            first_event_sequence_number,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>(),
            FailureKind.Handling);

    [Fact] void should_not_report_any_handled_events() => _observer.DidNotReceive().ReportHandledEvents(Arg.Any<Key>(), Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>());
}
