// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_getting;

public class and_the_resident_observer_has_not_sampled_yet : given.an_observer_lag_query
{
    ObserverLag _result;

    void Establish() => _grain.GetInformation().Returns(new ObserverInformationSnapshot(Definition, new ObserverState(), true, null));

    async Task Because() => _result = await Query();

    [Fact] void should_be_unmeasurable() => _result.Status.ShouldEqual(ObserverLagStatus.Unmeasurable);
    [Fact] void should_never_claim_a_distance_of_zero() => _result.Distance.ShouldBeNull();
}
