// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_removing;

public class and_a_namespace_outside_the_list_is_still_subscribed : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor("Default"));
    }

    async Task Because() => await _manager.Remove(SubscriptionName);

    [Fact] void should_unsubscribe_the_listed_namespace() => _observers[KeyFor("Default")].Received(1).Unsubscribe();
    [Fact] void should_unsubscribe_the_leftover_namespace() => _observers[KeyFor("Tenant1")].Received(1).Unsubscribe();
}
