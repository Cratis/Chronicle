// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_recovered;

public class and_the_observer_is_quarantined : for_Observer.given.a_quarantined_observer
{
    readonly Key _partition = "recovered-partition";

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
        _failedPartitionsStorage.State.Quarantine(_partition);
    }

    async Task Because()
    {
        await _observer.FailedPartitionRecovered(_partition, 42UL);
        await RunWatchdogTicks();
    }

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_resolve_the_failed_partition() => _failedPartitionsStorage.State.ResolvedPartitions.Single().Partition.ShouldEqual(_partition);
    [Fact] void should_record_progress() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_persist_failure_resolution() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_resubscribe() => ShouldNotHaveResubscribed();
}
