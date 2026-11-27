// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_getting;

public class and_the_observer_is_resident_with_a_sample : given.an_observer_lag_query
{
    ObserverLag _result;

    void Establish()
    {
        var sample = new ObserverLagSample(ObserverLagStatus.Stalled, ObserverLagCause.Unknown, 12UL, 5UL, 4UL, 5UL, 8UL, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(2));
        _grain.GetInformation().Returns(new ObserverInformationSnapshot(Definition, new ObserverState(), true, null) { LagSample = sample });
    }

    async Task Because() => _result = await Query();

    [Fact] void should_report_the_sample_status() => _result.Status.ShouldEqual(ObserverLagStatus.Stalled);
    [Fact] void should_report_the_distance() => _result.Distance.ShouldEqual(8UL);
    [Fact] void should_report_the_observer() => _result.ObserverId.ShouldEqual(Id.Value);
}
