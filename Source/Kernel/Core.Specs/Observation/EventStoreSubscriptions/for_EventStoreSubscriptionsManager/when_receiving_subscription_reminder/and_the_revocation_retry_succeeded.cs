// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_revocation_retry_succeeded : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _sourceHas.Add(Malformed);
        _observersAreSubscribed = true;
        _unsubscribeFailures[KeyFor(Malformed)] = 1;
        await _manager.Add(DefinitionFor());
        await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);
        _requestedKeys.Clear();
        _sourceNamespacesRequested.Clear();
    }

    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_not_repeat_the_successful_revocation() => _observers[KeyFor(Malformed)].Received(2).Unsubscribe();
    [Fact] void should_not_resolve_the_revoked_observer_again() => RequestedKeys.ShouldNotContain(KeyFor(Malformed));
    [Fact] void should_not_look_up_the_malformed_source_namespace() => _sourceNamespacesRequested.ShouldNotContain(Malformed);
}
