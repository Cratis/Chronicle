// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_lag;

public class and_catch_up_started_since_sampling : given.an_observer_with_a_stalled_sample
{
    ObserverLagSample _duringCatchUp;
    ObserverLagSample _afterCatchUp;

    async Task Because()
    {
        _stateStorage.State.CatchingUpPartitions.Add("catching-up");
        _duringCatchUp = (await _observer.GetInformation()).LagSample;
        _stateStorage.State.CatchingUpPartitions.Clear();
        await _observer.RunWatchdogAsync();
        _afterCatchUp = (await _observer.GetInformation()).LagSample;
    }

    [Fact] void should_report_lagging_instead_of_stalled() => _duringCatchUp.Status.ShouldEqual(ObserverLagStatus.Lagging);
    [Fact] void should_still_report_the_measured_distance() => _duringCatchUp.Distance.ShouldEqual(17UL);
    [Fact] void should_not_reset_the_stall_baseline_on_read() => _afterCatchUp.Status.ShouldEqual(ObserverLagStatus.Stalled);
}
