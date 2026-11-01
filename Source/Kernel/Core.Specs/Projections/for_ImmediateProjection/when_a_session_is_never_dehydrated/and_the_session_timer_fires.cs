// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_a_session_is_never_dehydrated;

public class and_the_session_timer_fires : given.a_session_immediate_projection
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_deactivate_the_session() => _grain.SessionDeactivated.ShouldBeTrue();
    [Fact] void should_not_keep_the_session_alive() => ImmediateProjection.SessionTimerOptions.KeepAlive.ShouldBeFalse();
    [Fact] void should_not_interleave() => ImmediateProjection.SessionTimerOptions.Interleave.ShouldBeFalse();
    [Fact] void should_expire_after_one_minute() => ImmediateProjection.SessionTimerOptions.DueTime.ShouldEqual(TimeSpan.FromMinutes(1));
    [Fact] void should_expire_only_once() => ImmediateProjection.SessionTimerOptions.Period.ShouldEqual(Timeout.InfiniteTimeSpan);
}
