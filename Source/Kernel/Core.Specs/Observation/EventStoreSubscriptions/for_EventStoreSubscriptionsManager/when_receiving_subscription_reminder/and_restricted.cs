// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_restricted : given.a_manager_over_several_namespaces
{
    async Task Establish() => await _manager.Add(DefinitionFor("Tenant2"));
    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_not_subscribe_a_namespace_outside_the_list() => WasSubscribed("Tenant1").ShouldBeFalse();
    [Fact] void should_subscribe_the_listed_namespace() => WasSubscribed("Tenant2").ShouldBeTrue();
}
