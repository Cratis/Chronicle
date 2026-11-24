// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_silo_leaves;

public class and_the_window_is_open : given.a_silo_supervisor
{
    int _first;
    int _second;
    int _noDuty;
    bool _openBefore;

    void Establish()
    {
        _supervisor.Register("first", _interval, Counting(() => { }), () =>
        {
            _first++;
            return Task.CompletedTask;
        });
        _supervisor.Register("second", null, null, () =>
        {
            _second++;
            return Task.CompletedTask;
        });
        _supervisor.Register("no-duty", _interval, Counting(() => _noDuty++), null);
        _openBefore = _supervisor.IsDepartureWindowOpen;
    }

    async Task Because()
    {
        _supervisor.OpenDepartureWindow();
        await _supervisor.RunDeparturePass(TimeSpan.Zero);
    }

    [Fact] void should_not_have_been_open_before_the_departure() => _openBefore.ShouldBeFalse();
    [Fact] void should_be_open() => _supervisor.IsDepartureWindowOpen.ShouldBeTrue();
    [Fact] void should_run_the_duty_of_every_grain_that_has_one() => (_first == 1 && _second == 1).ShouldBeTrue();
    [Fact] void should_not_run_the_periodic_check() => _noDuty.ShouldEqual(0);
}
