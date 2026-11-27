// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_lag;

public class and_the_distance_does_not_fit_a_signed_long : given.an_observer_publishing_a_lag_sample
{
    IReadOnlyList<RecordedMeasurement> _distance;
    IReadOnlyList<RecordedMeasurement> _stalled;

    void Establish() => Publish(Sample(ObserverLagStatus.Stalled, (ulong)long.MaxValue + 1));

    void Because()
    {
        _distance = Collect(DistanceInstrument);
        _stalled = Collect(StalledInstrument);
    }

    [Fact] void should_omit_the_distance_instead_of_wrapping_it() => _distance.ShouldBeEmpty();
    [Fact] void should_still_report_that_the_observer_is_stalled() => _stalled.Single().Value.ShouldEqual(1);
}
