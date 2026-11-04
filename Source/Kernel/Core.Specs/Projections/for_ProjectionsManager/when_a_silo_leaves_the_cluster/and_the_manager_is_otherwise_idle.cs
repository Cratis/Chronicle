// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_a_silo_leaves_the_cluster;

public class and_the_manager_is_otherwise_idle : Specification
{
    [Fact] void should_keep_the_manager_active_while_watching() => ProjectionsManager.SiloDepartureTimerOptions.KeepAlive.ShouldBeTrue();
    [Fact] void should_not_interleave_the_check() => ProjectionsManager.SiloDepartureTimerOptions.Interleave.ShouldBeFalse();
    [Fact] void should_check_every_ten_seconds() => ProjectionsManager.SiloDepartureTimerOptions.Period.ShouldEqual(TimeSpan.FromSeconds(10));
}
