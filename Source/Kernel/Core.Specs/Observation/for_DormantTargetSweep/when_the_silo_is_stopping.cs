// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_DormantTargetSweep;

public class when_the_silo_is_stopping : given.a_dormant_target_sweep
{
    int _woken;

    protected override bool SiloIsStopping => true;

    async Task Because()
    {
        await _sweep.ConnectionLeft(new ConnectionId(LeavingConnection), []);
        _woken = await _sweep.SilosLeft([LeavingSilo]);
    }

    [Fact] void should_tell_no_observer() => _heldObserver.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_wake_none() => _woken.ShouldEqual(0);
    [Fact] async Task should_not_read_storage_that_is_going_away() => await _records.DidNotReceive().GetAll(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<Concepts.EventSequences.EventSequenceId>(), Arg.Any<bool>());
}
