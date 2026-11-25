// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_check_is_due;

public class and_the_previous_check_is_still_running : given.a_silo_supervisor
{
    readonly TaskCompletionSource<TimeSpan?> _running = new();
    int _checks;

    void Establish()
    {
        Task<TimeSpan?> Slow()
        {
            _checks++;
            return _running.Task;
        }

        _supervisor.Register("slow", _interval, Slow, null);
        _clock.Advance(_interval);
    }

    async Task Because()
    {
        var first = _supervisor.RunDueWatchdogs();
        _clock.Advance(_interval);
        await _supervisor.RunDueWatchdogs();
        _running.SetResult(null);
        await first;
    }

    [Fact] void should_not_start_a_second_turn_over_the_first() => _checks.ShouldEqual(1);
}
