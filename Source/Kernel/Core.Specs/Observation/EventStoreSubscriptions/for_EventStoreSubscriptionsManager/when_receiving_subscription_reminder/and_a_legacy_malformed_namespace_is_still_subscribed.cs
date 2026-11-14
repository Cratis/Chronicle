// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_a_legacy_malformed_namespace_is_still_subscribed : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _sourceHas.Add(Malformed);
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
    }

    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_revoke_it() => _observers[KeyFor(Malformed)].Received(1).Unsubscribe();
}
