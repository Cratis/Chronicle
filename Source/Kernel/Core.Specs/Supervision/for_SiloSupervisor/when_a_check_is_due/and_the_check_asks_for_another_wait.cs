// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_check_is_due;

public class and_the_check_asks_for_another_wait : given.a_silo_supervisor
{
    int _checks;

    void Establish()
    {
        _supervisor.Register("a", _interval, Counting(() => _checks++, TimeSpan.FromSeconds(5)), null);
        _clock.Advance(_interval);
    }

    async Task Because()
    {
        await _supervisor.RunDueWatchdogs();
        _clock.Advance(TimeSpan.FromSeconds(5));
        await _supervisor.RunDueWatchdogs();
    }

    [Fact] void should_run_the_check_again_after_the_wait_it_asked_for() => _checks.ShouldEqual(2);
}
