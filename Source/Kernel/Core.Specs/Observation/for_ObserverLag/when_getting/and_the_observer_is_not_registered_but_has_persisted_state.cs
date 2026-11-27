// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_getting;

public class and_the_observer_is_not_registered_but_has_persisted_state : given.an_observer_lag_query
{
    ObserverLag _result;

    void Establish()
    {
        DirectoryReturns(null);
        var sample = new ObserverLagSample(ObserverLagStatus.Lagging, ObserverLagCause.Unknown, 12UL, 5UL, 4UL, 5UL, 8UL, null, null);
        _grain.GetInformation().Returns(new ObserverInformationSnapshot(Definition, new ObserverState(), true, null) { LagSample = sample });
    }

    async Task Because() => _result = await Query();

    [Fact] void should_report_no_sample() => _result.Cause.ShouldEqual(ObserverLagCause.NoSample);
    [Fact] async Task should_use_the_directory() => await _directory.Received(1).GetActivationAddress(_grain);
    [Fact] async Task should_not_wake_the_observer() => await _grain.DidNotReceive().GetInformation();
}
