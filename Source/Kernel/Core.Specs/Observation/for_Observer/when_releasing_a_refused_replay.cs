// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_releasing_a_refused_replay : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            IsReplaying = true,
            LastHandledEventSequenceNumber = 42UL,
            NextEventSequenceNumber = 45UL,
            FailedPartitionCount = 1
        };
        _stateStorage.State.ReplayingPartitions.Add("replaying");
        _stateStorage.State.CatchingUpPartitions.Add("catching-up");
        _failedPartitionsStorage.State.AddFailedPartition("failed", 12UL);
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.ReleaseRefusedReplay();
        await _observer.ReleaseRefusedReplay();
    }

    [Fact] void should_preserve_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_preserve_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)45UL);
    [Fact] void should_release_full_replay() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] void should_clear_partition_replay_markers() => _stateStorage.State.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_clear_superseded_catchup_markers() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_preserve_failures() => _failedPartitionsStorage.State.Partitions.Count().ShouldEqual(1);
    [Fact] void should_not_resolve_failures() => _failedPartitionsStorage.State.ResolvedPartitions.ShouldBeEmpty();
    [Fact] void should_preserve_failure_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] async Task should_route_from_the_retained_position() => await _eventSequence.Received(1).GetNextSequenceNumberGreaterOrEqualTo(45UL, Arg.Any<IEnumerable<EventType>>());
    [Fact] async Task should_resume_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_persist_release() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
