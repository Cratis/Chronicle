// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_lag;

public class and_the_sample_expires : given.an_observer_publishing_a_lag_sample
{
    IReadOnlyList<RecordedMeasurement> _atTheBoundary;
    IReadOnlyList<RecordedMeasurement> _afterTheBoundary;

    void Establish() => Publish(Sample(ObserverLagStatus.Stalled, 17));

    void Because()
    {
        _clock.Advance(_validity);
        _atTheBoundary = CollectLag();
        _clock.Advance(TimeSpan.FromTicks(1));
        _afterTheBoundary = CollectLag();
    }

    [Fact] void should_still_report_both_at_the_moment_of_expiry() => _atTheBoundary.Count.ShouldEqual(2);
    [Fact] void should_report_nothing_after_expiry_without_the_observer_publishing() => _afterTheBoundary.ShouldBeEmpty();
}
