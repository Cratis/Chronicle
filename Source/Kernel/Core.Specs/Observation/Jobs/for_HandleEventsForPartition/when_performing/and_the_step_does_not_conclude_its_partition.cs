// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// Replays, retries and single-partition catch-ups hand their partition back through their own job, so a step that is
/// not asked to conclude its partition reads once and leaves the observer alone.
/// </summary>
public class and_the_step_does_not_conclude_its_partition : given.a_performing_job_step
{
    void Establish() =>
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Ok(first_event_sequence_number)));

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_ask_the_observer_to_conclude_the_partition() => _observer.DidNotReceive().ConcludePartitionCatchUp(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>());
}
