// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_source_event_store_is_added;

public class and_restricted : given.a_manager_over_several_namespaces
{
    async Task Establish() => await _manager.Add(DefinitionFor("Default", "Tenant1"));
    async Task Because() => await _manager.SourceEventStoreAdded("Source");

    [Fact] void should_only_resolve_the_observers_of_the_listed_namespaces() => RequestedKeys.ShouldContainOnly(KeyFor("Default"), KeyFor("Tenant1"));
}
