// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_ensuring_with_nothing_pending : given.a_lifecycle_grain
{
    int _timers;

    async Task Establish() => await _silo.TimerRegistry.FireAllAsync();

    async Task Because()
    {
        await _grain.Ensure();
        _timers = _silo.TimerRegistry.NumberOfActiveTimers;
    }

    [Fact] void should_not_leave_a_timer_armed() => _timers.ShouldEqual(0);
}
