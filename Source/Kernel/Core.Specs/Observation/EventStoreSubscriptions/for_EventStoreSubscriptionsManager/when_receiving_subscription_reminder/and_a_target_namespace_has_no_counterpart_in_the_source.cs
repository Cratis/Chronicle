// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_a_target_namespace_has_no_counterpart_in_the_source : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _sourceHas.Remove("Tenant2");
        await _manager.Add(DefinitionFor());
        _requestedKeys.Clear();
    }

    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_attach_to_the_namespaces_the_source_has() => RequestedKeys.ShouldContainOnly(KeyFor("Default"), KeyFor("Tenant1"));
    [Fact] void should_never_resolve_an_observer_that_would_create_the_namespace_in_the_source() => RequestedKeys.ShouldNotContain(KeyFor("Tenant2"));
}
