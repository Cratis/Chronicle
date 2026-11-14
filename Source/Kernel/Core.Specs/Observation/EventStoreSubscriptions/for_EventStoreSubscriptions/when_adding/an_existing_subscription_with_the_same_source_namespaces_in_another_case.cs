// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class an_existing_subscription_with_the_same_source_namespaces_in_another_case : given.a_service_that_can_add
{
    void Establish() => _existing = [ExistingWith("DEFAULT", "tenant1")];

    async Task Because() => await _service.Add(RequestWith("Tenant1", "Default"));

    [Fact] void should_not_register_it_again() => _subscriptionsManager.DidNotReceiveWithAnyArgs().Add(default!);
}
