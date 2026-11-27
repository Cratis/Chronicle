// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_replaying_a_partition_again;

public class and_the_active_job_is_refused : given.an_observer
{
    static readonly Key _partition = "mutated-source";
    readonly List<ReplayObserverPartitionRequest> _starts = [];
    int _startsBeforeRelease;

    void Establish() => _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Do<ReplayObserverPartitionRequest>(_starts.Add));

    async Task Because()
    {
        // A bounded replay does not cover a subsequently requested full repair.
        await _observer.ReplayPartitionTo(_partition, EventSequenceNumber.First);
        var active = _starts.Single();
        await _observer.ReplayPartition(_partition);
        _startsBeforeRelease = _starts.Count;
        await _observer.ReleaseRefusedPartitionReplay(_partition, active.HeldBackFrom, active.OwnershipEpoch);
    }

    [Fact] void should_not_start_the_follow_up_before_refusal() => _startsBeforeRelease.ShouldEqual(1);
    [Fact] void should_start_one_full_follow_up() => _starts.Skip(1).Select(_ => _.ToSequenceNumber).ShouldEqual(new[] { EventSequenceNumber.Max });
    [Fact] void should_keep_live_delivery_held_for_the_follow_up() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
}
