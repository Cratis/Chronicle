// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_lag;

public class and_the_observer_is_stalled : given.an_observer_publishing_a_lag_sample
{
    IReadOnlyList<RecordedMeasurement> _distance;
    IReadOnlyList<RecordedMeasurement> _stalled;

    void Establish() => Publish(Sample(ObserverLagStatus.Stalled, 17));

    void Because()
    {
        _distance = Collect(DistanceInstrument);
        _stalled = Collect(StalledInstrument);
    }

    [Fact] void should_report_the_distance() => _distance.Single().Value.ShouldEqual(17);
    [Fact] void should_report_stalled() => _stalled.Single().Value.ShouldEqual(1);
}
