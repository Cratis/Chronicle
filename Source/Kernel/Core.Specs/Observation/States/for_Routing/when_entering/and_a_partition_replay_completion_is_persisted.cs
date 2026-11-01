// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay.given;

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_a_partition_replay_completion_is_persisted : a_partition_replay_covering_a_failure
{
    bool _heldBeforeRecovery;

    void Establish() => _failureStorage.FailOnWrite = 1;

    async Task Because()
    {
        await CompleteWithFault();
        await Crash();
        _heldBeforeRecovery = (await _observer.GetState()).ReplayingPartitions.Contains(_partition);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero);
    }

    [Fact] void should_restore_the_hold_without_an_outstanding_job() => _heldBeforeRecovery.ShouldBeTrue();
    [Fact] void should_admit_the_successor_after_routing() => CheckSuccessor();
}
