// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class without_a_linked_definition : given.a_pinned_subscriber
{
    void Establish() => _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns([]);

    async Task Because() => _result = await Forward();

    [Fact] void should_fail_closed_and_not_forward() => AppendCalls().ShouldEqual(0);
    [Fact] void should_acknowledge_and_move_past_the_events() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
