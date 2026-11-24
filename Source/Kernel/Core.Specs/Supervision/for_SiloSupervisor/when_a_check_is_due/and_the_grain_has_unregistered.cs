// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_check_is_due;

public class and_the_grain_has_unregistered : given.a_silo_supervisor
{
    int _checks;

    void Establish()
    {
        _supervisor.Register("a", _interval, Counting(() => _checks++), null);
        _supervisor.Unregister("a");
        _clock.Advance(_interval);
    }

    async Task Because() => await _supervisor.RunDueWatchdogs();

    [Fact] void should_not_run_the_check() => _checks.ShouldEqual(0);
    [Fact] void should_hold_no_registrations() => _supervisor.Count.ShouldEqual(0);
}
