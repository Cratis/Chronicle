// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_subscriber_disconnects_with_no_prior_handled_events : given.a_performing_job_step
{
    void Establish() =>
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Disconnected()));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_partition_failed_with_first_event_sequence_number() =>
        _observer.Received(1).PartitionFailed(
            Arg.Any<Key>(),
            first_event_sequence_number,
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<string>());
}
