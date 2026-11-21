// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_listed_source_namespace_in_another_case : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    protected override EventStoreNamespaceName SourceNamespace => "TENANT1";

    void Establish()
    {
        var definition = new EventStoreSubscriptionDefinition("subscription", "Admin", [new EventType("event-type-1", 1)])
        {
            SourceNamespaces = ["Default", "Tenant1"]
        };
        _storage.GetEventStore("Lobby").EventStoreSubscriptions.GetAll().Returns(_ => [definition]);
    }

    async Task Because() => _result = await Forward();

    [Fact] void should_forward_every_event() => AppendCalls().ShouldEqual(3);
    [Fact] void should_report_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
