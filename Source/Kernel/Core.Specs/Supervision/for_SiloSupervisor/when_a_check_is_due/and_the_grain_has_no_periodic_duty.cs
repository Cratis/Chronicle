// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_check_is_due;

public class and_the_grain_has_no_periodic_duty : given.a_silo_supervisor
{
    bool _afterDeparture;

    void Establish()
    {
        _supervisor.Register("a", null, null, () =>
        {
            _afterDeparture = true;
            return Task.CompletedTask;
        });
        _clock.Advance(TimeSpan.FromDays(1));
    }

    async Task Because() => await _supervisor.RunDueWatchdogs();

    [Fact] void should_not_run_anything() => _afterDeparture.ShouldBeFalse();
}
