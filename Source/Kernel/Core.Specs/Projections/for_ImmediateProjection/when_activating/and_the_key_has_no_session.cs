// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_activating;

public class and_the_key_has_no_session : given.an_immediate_projection
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_not_register_a_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_not_deactivate() => _grain.SessionDeactivated.ShouldBeFalse();
}
