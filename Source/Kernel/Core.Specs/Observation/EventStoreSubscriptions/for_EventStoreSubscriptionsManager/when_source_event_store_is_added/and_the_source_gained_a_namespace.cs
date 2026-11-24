// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_source_event_store_is_added;

public class and_the_source_gained_a_namespace : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _sourceHas.Remove("Tenant2");
        await _manager.Add(DefinitionFor());
        await _manager.SourceEventStoreAdded("Source");
        _requestedKeys.Clear();
        _sourceHas.Add("Tenant2");
    }

    async Task Because() => await _manager.SourceEventStoreAdded("Source");

    [Fact] void should_attach_to_the_namespace_that_arrived() => WasSubscribed("Tenant2").ShouldBeTrue();
    [Fact] void should_have_attached_to_the_namespaces_it_already_had() => WasSubscribed("Tenant1").ShouldBeTrue();
}
