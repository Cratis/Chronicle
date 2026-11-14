// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_revocation_retry_succeeded : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
        _unsubscribeFailures[KeyFor("Tenant2")] = 1;
        await _manager.Add(DefinitionFor("Default"));
        await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);
        _requestedKeys.Clear();
        _sourceNamespacesRequested.Clear();
    }

    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_not_repeat_the_successful_revocation() => _observers[KeyFor("Tenant2")].Received(2).Unsubscribe();
    [Fact] void should_not_resolve_the_revoked_observer_again() => RequestedKeys.ShouldNotContain(KeyFor("Tenant2"));
    [Fact] void should_not_look_up_excluded_source_namespaces() => _sourceNamespacesRequested.ShouldBeEmpty();
}
