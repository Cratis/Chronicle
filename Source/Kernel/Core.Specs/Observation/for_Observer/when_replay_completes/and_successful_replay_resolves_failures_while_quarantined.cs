// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_successful_replay_resolves_failures_while_quarantined : given.a_quarantined_observer
{
    readonly Key _recoveredPartition = "recovered-partition";
    readonly Key _excludedPartition = "excluded-partition";
    readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true, FailedPartitionCount = 2 };
        _failedPartitionsStorage.State.AddFailedPartition(_recoveredPartition, 12UL);
        _failedPartitionsStorage.State.AddFailedPartition(_excludedPartition, 17UL);
        _failedPartitionsStorage.State.Quarantine(_recoveredPartition);
        GivenFailedEventAt(_recoveredPartition, 12UL, _eventType);
        GivenFailedEventAt(_excludedPartition, 17UL, EventType.Unknown);
    }

    async Task Because()
    {
        await _observer.ReplayedSuccessfullySince(42UL,
            new Dictionary<Key, EventSequenceNumber> { [_recoveredPartition] = 42UL, [_excludedPartition] = 42UL },
            [_eventType], DateTimeOffset.UtcNow.AddMinutes(1));
        await RunWatchdogTicks();
    }

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_resolve_only_the_covered_failure() => _failedPartitionsStorage.State.ResolvedPartitions.Single().Partition.ShouldEqual(_recoveredPartition);
    [Fact] void should_keep_the_excluded_failure() => _failedPartitionsStorage.State.Partitions.Single().Partition.ShouldEqual(_excludedPartition);
    [Fact] void should_count_the_remaining_failure() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] void should_persist_failure_resolution() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_finish_replay() => _stateStorage.State.IsReplaying.ShouldBeFalse();
    [Fact] void should_record_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_restart_replay() => ShouldNotHaveStartedReplay();
    [Fact] void should_not_resubscribe() => ShouldNotHaveResubscribed();
}
