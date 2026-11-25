// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_check_is_due;

public class and_the_interval_has_not_passed : given.a_silo_supervisor
{
    int _checks;

    void Establish()
    {
        _supervisor.Register("a", _interval, Counting(() => _checks++), null);
        _clock.Advance(_interval - TimeSpan.FromSeconds(1));
    }

    async Task Because() => await _supervisor.RunDueWatchdogs();

    [Fact] void should_not_run_the_check() => _checks.ShouldEqual(0);
}
