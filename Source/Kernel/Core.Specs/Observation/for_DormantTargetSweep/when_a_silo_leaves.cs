// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DormantTargetSweep;

public class when_a_silo_leaves : given.a_dormant_target_sweep
{
    int _woken;

    async Task Because() => _woken = await _sweep.SilosLeft([LeavingSilo]);

    [Fact] void should_wake_the_observer_that_recorded_a_client_served_from_it_to_validate_its_record() => _heldObserver.Received(1).RunWatchdogOnce();
    [Fact] void should_leave_observers_served_from_other_silos_alone() => _otherObserver.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_report_how_many_it_woke() => _woken.ShouldEqual(1);
    [Fact] void should_give_the_wake_places_back() => _wakeAdmission.InProgress.ShouldEqual(0);
}
