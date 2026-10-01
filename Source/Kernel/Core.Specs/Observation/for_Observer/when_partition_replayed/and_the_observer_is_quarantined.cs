// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed;

public class and_the_observer_is_quarantined : given.an_observer_with_replaying_partition
{
    async Task Establish()
    {
        _observer.SetSubscription(new(_observerId, _observerKey, [EventType.Unknown], typeof(ObserverSubscriber), SiloAddress.Zero, null));
        await _observer.TransitionTo<Routing>();
        await _observer.TransitionTo<QuarantinedObserver>();
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 45UL, FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        GivenFailedEventAt(_partition, 12UL, EventType.Unknown);
        _storageStats.ResetCounts();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, [EventType.Unknown]);
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_clear_the_completed_partition() => _stateStorage.State.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_resolve_the_covered_failure() => _failedPartitionsStorage.State.ResolvedPartitions.Single().Partition.ShouldEqual(_partition);
    [Fact] void should_record_last_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_preserve_the_higher_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)45UL);
    [Fact] void should_persist_completion() => _storageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_not_start_unneeded_catchup() => CheckDidNotStartCatchupJob();
}
