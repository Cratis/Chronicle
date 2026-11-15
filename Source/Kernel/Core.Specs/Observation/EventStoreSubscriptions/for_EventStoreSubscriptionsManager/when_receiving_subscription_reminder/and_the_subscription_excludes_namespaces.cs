// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_subscription_excludes_namespaces : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor("Default"));
        _requestedKeys.Clear();
        _sourceNamespacesRequested.Clear();
    }

    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_not_resolve_the_first_excluded_observer() => RequestedKeys.ShouldNotContain(KeyFor("Tenant1"));
    [Fact] void should_not_resolve_the_second_excluded_observer() => RequestedKeys.ShouldNotContain(KeyFor("Tenant2"));
    [Fact] void should_not_resolve_the_malformed_namespace_observer() => RequestedKeys.ShouldNotContain(KeyFor(Malformed));
    [Fact] void should_not_look_up_excluded_source_namespaces() => _sourceNamespacesRequested.ShouldBeEmpty();
    [Fact] void should_still_check_the_observed_namespace() => RequestedKeys.ShouldContain(KeyFor("Default"));
}
