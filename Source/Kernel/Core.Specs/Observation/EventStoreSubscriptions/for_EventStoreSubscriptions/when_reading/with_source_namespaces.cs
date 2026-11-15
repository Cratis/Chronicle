// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_reading;

public class with_source_namespaces : given.an_event_store_subscriptions_service
{
    IEnumerable<Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition> _result;

    void Establish() => _storage.GetEventStore("target").EventStoreSubscriptions.GetAll().Returns([
        new Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition("subscription", "source", [])
        {
            SourceNamespaces = [new Concepts.EventStoreNamespaceName("Default")]
        }]);

    async Task Because() => _result = await _service.GetSubscriptions(new GetEventStoreSubscriptionsRequest { TargetEventStore = "target" });

    [Fact] void should_expose_the_source_namespaces() => _result.Single().SourceNamespaces.ShouldContainOnly("Default");
}
