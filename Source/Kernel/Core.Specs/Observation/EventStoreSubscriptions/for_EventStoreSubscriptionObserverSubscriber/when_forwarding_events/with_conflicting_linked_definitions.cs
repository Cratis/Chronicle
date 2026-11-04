// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_conflicting_linked_definitions : given.a_pinned_subscriber
{
    void Establish() => _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns([_definition, _definition]);

    async Task Because() => _result = await Forward();

    [Fact] void should_not_choose_an_arbitrary_linked_definition() => AppendCalls().ShouldEqual(0);
    [Fact] void should_report_failed_delivery() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
}
