// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_check_is_due;

public class and_a_check_fails : given.a_silo_supervisor
{
    int _checksOfTheHealthyGrain;
    int _failingChecks;

    void Establish()
    {
        Task<TimeSpan?> Fail()
        {
            _failingChecks++;
            return Task.FromException<TimeSpan?>(new InvalidOperationException("boom"));
        }

        _supervisor.Register("failing", _interval, Fail, null);
        _supervisor.Register("healthy", _interval, Counting(() => _checksOfTheHealthyGrain++), null);
        _clock.Advance(_interval);
    }

    async Task Because()
    {
        await _supervisor.RunDueWatchdogs();
        _clock.Advance(_interval);
        await _supervisor.RunDueWatchdogs();
    }

    [Fact] void should_still_run_the_check_of_the_other_grain() => _checksOfTheHealthyGrain.ShouldEqual(2);
    [Fact] void should_run_the_failing_check_again_at_the_next_interval() => _failingChecks.ShouldEqual(2);
}
