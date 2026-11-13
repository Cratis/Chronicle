// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_removing;

public class and_restricted : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor("Tenant1"));
    }

    async Task Because() => await _manager.Remove(SubscriptionName);

    [Fact] void should_only_resolve_the_observer_of_the_listed_namespace() => RequestedKeys.ShouldContainOnly(KeyFor("Tenant1"));
    [Fact] void should_unsubscribe_the_listed_namespace() => _observers[KeyFor("Tenant1")].Received(1).Unsubscribe();
}
