// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_subscription_is_healthy_in_several_namespaces : given.a_manager_over_several_namespaces
{
    static readonly string[] _observed = ["Default", "Tenant1", "Tenant2"];

    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
    }

    async Task Because() => await _manager.ReceiveReminder($"event-store-subscription-subscribe:{SubscriptionName}", default);

    [Fact] void should_not_subscribe_any_namespace() => _observed.Any(WasSubscribed).ShouldBeFalse();
    [Fact] void should_not_unsubscribe_any_namespace() => _observed.Any(name => CallsTo(name, nameof(IObserver.Unsubscribe)) > 0).ShouldBeFalse();
    [Fact] void should_ask_every_observed_observer_whether_it_is_subscribed() => _observed.All(name => CallsTo(name, nameof(IObserver.IsSubscribed)) >= 1).ShouldBeTrue();
    [Fact] void should_not_resolve_the_malformed_namespace_observer() => RequestedKeys.ShouldNotContain(KeyFor(Malformed));

    int CallsTo(string @namespace, string method) =>
        _observers[KeyFor(@namespace)].ReceivedCalls().Count(call => call.GetMethodInfo().Name == method);
}
