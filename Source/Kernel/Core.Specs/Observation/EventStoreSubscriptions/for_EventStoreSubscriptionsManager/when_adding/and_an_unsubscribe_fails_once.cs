// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_adding;

public class and_an_unsubscribe_fails_once : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
        _unsubscribeFailures[KeyFor("Tenant2")] = 1;
    }

    async Task Because() => await _manager.Add(DefinitionFor("Default"));

    [Fact] void should_still_revoke_the_other_namespace() => _observers[KeyFor("Tenant1")].Received(1).Unsubscribe();
    [Fact] void should_have_tried_the_failing_namespace() => _observers[KeyFor("Tenant2")].Received(1).Unsubscribe();
}
