// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_namespace_is_added;

public class and_the_source_has_the_namespace : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _sourceHas.Add("Tenant9");
        await _manager.Add(DefinitionFor());
        await AttachNamespaceAdded();
    }

    async Task Because() => await _onNamespaceAdded(new("Target", "tenant9"));

    [Fact] void should_subscribe_the_namespace() => WasSubscribed("tenant9").ShouldBeTrue();
}
