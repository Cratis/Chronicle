// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class an_existing_subscription_with_different_source_namespaces : given.a_service_that_can_add
{
    void Establish() => _existing = [ExistingWith()];

    async Task Because() => await _service.Add(RequestWith("Default"));

    [Fact] void should_register_the_narrowed_definition() =>
        _subscriptionsManager.Received(1).Add(Arg.Is<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>(definition => definition.SourceNamespaces.Count() == 1));
}
