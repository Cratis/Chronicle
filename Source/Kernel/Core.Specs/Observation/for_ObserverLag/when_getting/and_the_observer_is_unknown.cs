// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_getting;

public class and_the_observer_is_unknown : given.an_observer_lag_query
{
    ObserverLag _result;

    void Establish() => _storage.GetEventStore(Store).Observers.Has(Id).Returns(false);

    async Task Because() => _result = await Query();

    [Fact] void should_be_unmeasurable() => _result.Status.ShouldEqual(ObserverLagStatus.Unmeasurable);
    [Fact] void should_not_claim_a_distance() => _result.Distance.ShouldBeNull();
    [Fact] async Task should_not_reach_a_grain() => await _grain.DidNotReceive().GetInformation();
}
