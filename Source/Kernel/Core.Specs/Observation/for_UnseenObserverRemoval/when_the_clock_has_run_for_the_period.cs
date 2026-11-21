// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval;

public class when_the_clock_has_run_for_the_period : given.an_unseen_observer_removal
{
    int _firstSweep;
    int _secondSweep;

    async Task Because()
    {
        _firstSweep = await _removal.Sweep();
        _time.Now = _time.Now.AddDays(7).AddMinutes(1);
        _secondSweep = await _removal.Sweep();
    }

    [Fact] void should_not_remove_an_observer_from_before_the_upgrade_on_the_first_sweep() => _firstSweep.ShouldEqual(0);
    [Fact] void should_remove_it_once_the_period_has_passed_since_the_clock_started() => _secondSweep.ShouldEqual(1);
    [Fact] void should_remove_that_observer() => _removed.ShouldContain("never-seen");

    protected override Task Seed() => Observer("never-seen", null, null);
}
