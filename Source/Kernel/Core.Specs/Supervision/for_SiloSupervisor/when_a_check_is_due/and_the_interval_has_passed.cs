// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_check_is_due;

public class and_the_interval_has_passed : given.a_silo_supervisor
{
    int _checks;
    int _checksOfAnotherGrain;

    void Establish()
    {
        _supervisor.Register("a", _interval, Counting(() => _checks++), null);
        _supervisor.Register("b", _interval * 2, Counting(() => _checksOfAnotherGrain++), null);
        _clock.Advance(_interval);
    }

    async Task Because() => await _supervisor.RunDueWatchdogs();

    [Fact] void should_run_the_check_that_is_due() => _checks.ShouldEqual(1);
    [Fact] void should_not_run_the_check_that_is_not_due_yet() => _checksOfAnotherGrain.ShouldEqual(0);
}
