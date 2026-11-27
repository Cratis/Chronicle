// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverLag.when_getting;

public class and_the_observer_is_dormant : given.an_observer_lag_query
{
    ObserverLag _result;

    void Establish() => _dormant.Find(_key).Returns(new ObserverDurableSubscription(ObserverSubscriptionKind.EventStoreSubscription, false, null, []));

    async Task Because() => _result = await Query();

    [Fact] void should_be_unmeasurable() => _result.Status.ShouldEqual(ObserverLagStatus.Unmeasurable);
    [Fact] void should_say_there_is_no_sample() => _result.Cause.ShouldEqual(ObserverLagCause.NoSample);
    [Fact] async Task should_not_wake_the_observer() => await _grain.DidNotReceive().GetInformation();
}
