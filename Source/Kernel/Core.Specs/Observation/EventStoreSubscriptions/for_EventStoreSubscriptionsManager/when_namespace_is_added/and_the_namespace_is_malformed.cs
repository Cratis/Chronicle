// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_namespace_is_added;

public class and_the_namespace_is_malformed : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        await _manager.Add(DefinitionFor());
        await AttachNamespaceAdded();
    }

    async Task Because() => await _onNamespaceAdded(new("Target", Malformed));

    [Fact] void should_not_resolve_any_observer() => RequestedKeys.ShouldBeEmpty();
}
