// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_adding;

public class and_a_legacy_malformed_namespace_has_an_active_observer : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        // An observer that predates the refusal of comma names is already running in the source.
        _sourceHas.Add(Malformed);
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
        _requestedKeys.Clear();
    }

    async Task Because() => await _manager.Add(DefinitionFor("Default"));

    [Fact] void should_revoke_the_malformed_namespace_observer() => _observers[KeyFor(Malformed)].Received(1).Unsubscribe();
    [Fact] void should_revoke_the_namespaces_outside_the_list() => _observers[KeyFor("Tenant1")].Received(1).Unsubscribe();
    [Fact] void should_keep_the_listed_namespace() => _requestedKeys.ShouldNotContain(KeyFor("Default"));
}
