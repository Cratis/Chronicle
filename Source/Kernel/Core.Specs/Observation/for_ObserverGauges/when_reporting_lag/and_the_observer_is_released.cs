// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_lag;

public class and_the_observer_is_released : given.an_observer_publishing_a_lag_sample
{
    IReadOnlyList<RecordedMeasurement> _afterRelease;
    IReadOnlyList<RecordedMeasurement> _afterOldRelease;

    void Establish() => Publish(Sample(ObserverLagStatus.Stalled, 17));

    void Because()
    {
        _entry.Release();
        _afterRelease = CollectLag();

        var replacement = _gauges.Register(_observerKey, ObserverType.Reactor);
        replacement.Publish(new(ObserverRunningState.Active, 0, 0) { LagSample = Sample(ObserverLagStatus.Lagging, 4) });
        _entry.Release();
        _afterOldRelease = Collect(DistanceInstrument);
    }

    [Fact] void should_leave_no_zero_tail() => _afterRelease.ShouldBeEmpty();
    [Fact] void should_keep_what_a_newer_activation_published_when_an_old_one_releases() => _afterOldRelease.Single().Value.ShouldEqual(4);
}
