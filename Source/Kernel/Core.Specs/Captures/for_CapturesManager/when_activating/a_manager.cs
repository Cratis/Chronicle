// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_activating;

public class a_manager : given.a_captures_manager
{
    [Fact] void should_register_the_reconciliation_with_the_supervisor() => _supervisor.Received(1).Register(
        $"captures:{_eventStore}",
        CapturesManager.ReconcileInterval,
        Arg.Any<Func<Task<TimeSpan?>>>(),
        Arg.Any<Func<Task>>());

    [Fact] void should_not_arm_a_timer_of_its_own() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
