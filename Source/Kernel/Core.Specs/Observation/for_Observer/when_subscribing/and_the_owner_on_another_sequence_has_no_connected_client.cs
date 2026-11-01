// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_the_owner_on_another_sequence_has_no_connected_client : given.an_observer_with_a_versioned_owner_on_another_sequence
{
    void Establish() => _owningObserver.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
    async Task Because() => await Register();

    [Fact] void should_move_the_definition() => _definitionStorage.State.EventSequenceId.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_not_recommend() => ShouldNotRecommend();
}
