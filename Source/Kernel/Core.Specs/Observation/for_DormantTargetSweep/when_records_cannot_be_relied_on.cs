// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_DormantTargetSweep;

public class when_records_cannot_be_relied_on : given.a_dormant_target_sweep
{
    int _woken;

    protected override bool RecordsCanBeRelied => false;

    async Task Because()
    {
        await _sweep.ConnectionLeft(new ConnectionId(LeavingConnection), []);
        _woken = await _sweep.SilosLeft([LeavingSilo]);
    }

    [Fact] void should_tell_no_observer() => _heldObserver.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_wake_none() => _woken.ShouldEqual(0);
}
