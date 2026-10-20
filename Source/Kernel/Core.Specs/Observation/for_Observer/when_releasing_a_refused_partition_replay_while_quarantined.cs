// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_releasing_a_refused_partition_replay_while_quarantined : given.a_quarantined_observer
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 42UL, NextEventSequenceNumber = 45UL };
        _stateStorage.State.ReplayingPartitions.Add("partition");
    }

    async Task Because() => await _observer.ReleaseRefusedPartitionReplay("partition");

    [Fact] void should_preserve_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_preserve_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)45UL);
    [Fact] void should_clear_the_partition_marker() => _stateStorage.State.ReplayingPartitions.ShouldBeEmpty();
    [Fact] async Task should_remain_quarantined() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_not_start_catchup() => CheckDidNotStartCatchupJob();
}
