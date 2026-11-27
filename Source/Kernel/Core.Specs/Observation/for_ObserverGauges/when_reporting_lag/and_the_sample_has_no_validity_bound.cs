// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_lag;

public class and_the_sample_has_no_validity_bound : given.an_observer_publishing_a_lag_sample
{
    IReadOnlyList<RecordedMeasurement> _measurements;

    void Establish() => Publish(Sample(ObserverLagStatus.Lagging, 17) with { ExpiresAt = null });

    void Because() => _measurements = CollectLag();

    [Fact] void should_not_treat_it_as_fresh() => _measurements.ShouldBeEmpty();
}
