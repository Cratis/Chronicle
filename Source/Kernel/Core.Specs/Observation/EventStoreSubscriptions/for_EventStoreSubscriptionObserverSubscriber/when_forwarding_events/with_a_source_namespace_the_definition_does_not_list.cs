// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_source_namespace_the_definition_does_not_list : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    protected override EventStoreNamespaceName SourceNamespace => "Tenant2";

    void Establish()
    {
        var definition = new EventStoreSubscriptionDefinition("subscription", "Admin", [new EventType("event-type-1", 1)])
        {
            SourceNamespaces = ["Default", "Tenant1"]
        };
        _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns(_ => [definition]);
    }

    async Task Because() => _result = await Forward();

    [Fact] void should_not_forward_any_event() => AppendCalls().ShouldEqual(0);
    [Fact] void should_report_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_move_past_the_dropped_events() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)44UL);
}
