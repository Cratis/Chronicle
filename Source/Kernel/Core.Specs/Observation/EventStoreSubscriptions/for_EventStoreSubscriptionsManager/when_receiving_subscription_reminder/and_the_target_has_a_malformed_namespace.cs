// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_target_has_a_malformed_namespace : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
        _requestedKeys.Clear();
        _sourceNamespacesRequested.Clear();
    }

    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_not_resolve_the_malformed_namespace_observer() => RequestedKeys.ShouldNotContain(KeyFor(Malformed));
    [Fact] void should_not_look_up_the_malformed_source_namespace() => _sourceNamespacesRequested.ShouldNotContain(Malformed);
    [Fact] void should_still_check_every_well_formed_namespace() => RequestedKeys.ShouldContainOnly(KeyFor("Default"), KeyFor("Tenant1"), KeyFor("Tenant2"));
}
