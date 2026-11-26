// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_only_one_of_several_namespaces_is_not_subscribed : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());

        // Resolve the observers once, then let one of them lose its subscription.
        await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);
        _observers[KeyFor("Tenant1")].IsSubscribed().Returns(false);
        foreach (var observer in _observers.Values)
        {
            observer.ClearReceivedCalls();
        }
    }

    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_subscribe_the_namespace_that_is_not_subscribed() => WasSubscribed("Tenant1").ShouldBeTrue();
    [Fact] void should_not_subscribe_the_default_namespace() => WasSubscribed("Default").ShouldBeFalse();
    [Fact] void should_not_subscribe_the_other_tenant() => WasSubscribed("Tenant2").ShouldBeFalse();
}
