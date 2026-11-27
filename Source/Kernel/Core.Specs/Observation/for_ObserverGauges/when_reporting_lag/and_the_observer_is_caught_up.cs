// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_lag;

public class and_the_observer_is_caught_up : given.an_observer_publishing_a_lag_sample
{
    IReadOnlyList<RecordedMeasurement> _distance;
    IReadOnlyList<RecordedMeasurement> _stalled;

    void Establish() => Publish(Sample(ObserverLagStatus.CaughtUp, 0));

    void Because()
    {
        _distance = Collect(DistanceInstrument);
        _stalled = Collect(StalledInstrument);
    }

    [Fact] void should_report_a_distance_of_zero_instead_of_nothing() => _distance.Single().Value.ShouldEqual(0);
    [Fact] void should_report_not_stalled() => _stalled.Single().Value.ShouldEqual(0);
}
