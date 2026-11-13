// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_adding;

public class and_the_restriction_narrows : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
        _requestedKeys.Clear();
    }

    async Task Because() => await _manager.Add(DefinitionFor("Default"));

    [Fact] void should_only_resolve_observers_of_namespaces_no_longer_observed_that_exist_in_the_source() => RequestedKeys.ShouldContainOnly(KeyFor("Tenant1"), KeyFor("Tenant2"));
    [Fact] void should_not_resolve_an_observer_for_the_malformed_namespace_the_source_lacks() => _requestedKeys.ShouldNotContain(KeyFor(Malformed));
    [Fact] void should_unsubscribe_a_namespace_no_longer_observed() => _observers[KeyFor("Tenant1")].Received(1).Unsubscribe();
    [Fact] void should_keep_the_observed_namespace() => _requestedKeys.ShouldNotContain(KeyFor("Default"));
}
