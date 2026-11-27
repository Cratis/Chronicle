// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_lag;

public class and_the_observer_became_ineligible_since_sampling : given.an_observer_with_a_stalled_sample
{
    ObserverLagSample _sample;

    async Task Because()
    {
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Disconnected };
        _sample = (await _observer.GetInformation()).LagSample;
    }

    [Fact] void should_report_unmeasurable() => _sample.Status.ShouldEqual(ObserverLagStatus.Unmeasurable);
    [Fact] void should_report_the_current_ineligibility() => _sample.Cause.ShouldEqual(ObserverLagCause.Inactive);
    [Fact] void should_not_report_the_stale_distance() => _sample.Distance.ShouldBeNull();
}
