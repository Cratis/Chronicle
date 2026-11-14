// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_removing;

public class after_a_failed_narrowing : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
        _unsubscribeFailures[KeyFor("Tenant1")] = 2;
        await _manager.Add(DefinitionFor("Default"));
    }

    async Task Because() => await _manager.Remove(SubscriptionName);

    [Fact] void should_still_have_tried_the_failing_namespace_when_removing() => _observers[KeyFor("Tenant1")].Received(2).Unsubscribe();
    [Fact] void should_not_throw_on_the_failure() => _unsubscribeFailures[KeyFor("Tenant1")].ShouldEqual(0);
}
