// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// The observer keeps finding an unread event for the partition, but reading on delivers nothing - an event of a type
/// this step does not read, for instance. Asking again would only spin, so the step stops asking and completes.
/// </summary>
public class and_reading_on_finds_nothing_the_observer_found : given.a_performing_job_step
{
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.ConcludesPartitionCatchUp = true;
        _observer.ConcludePartitionCatchUp(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(false);
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Ok(first_event_sequence_number)));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_ask_the_observer_only_once() => _observer.Received(1).ConcludePartitionCatchUp(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_complete() => _result.TryGetResult(out _).ShouldBeTrue();
}
