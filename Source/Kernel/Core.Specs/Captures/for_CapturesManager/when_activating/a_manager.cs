// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_activating;

public class a_manager : given.a_captures_manager
{
    [Fact] void should_reconcile_started_captures_on_a_timer_of_its_own() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
