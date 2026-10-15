// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.when_activating;

public class should_use_a_keep_alive_non_interleaved_timer : Specification
{
    [Fact] void should_keep_the_grain_alive() => AlertIncidentsMetricsOwner.TimerOptions.KeepAlive.ShouldBeTrue();
    [Fact] void should_not_interleave() => AlertIncidentsMetricsOwner.TimerOptions.Interleave.ShouldBeFalse();
    [Fact] void should_start_immediately() => AlertIncidentsMetricsOwner.TimerOptions.DueTime.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_refresh_periodically() => AlertIncidentsMetricsOwner.TimerOptions.Period.ShouldEqual(AlertIncidentsGaugeTiming.RefreshPeriod);
}
