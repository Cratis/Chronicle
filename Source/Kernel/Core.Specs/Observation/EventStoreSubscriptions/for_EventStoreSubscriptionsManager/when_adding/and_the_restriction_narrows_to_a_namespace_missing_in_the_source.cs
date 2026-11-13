// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_adding;

public class and_the_restriction_narrows_to_a_namespace_missing_in_the_source : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        _sourceHasNamespaces = false;
        await _manager.Add(DefinitionFor());
        _requestedKeys.Clear();
    }

    async Task Because() => await _manager.Add(DefinitionFor("Default"));

    [Fact] void should_never_resolve_an_observer_for_a_namespace_the_source_lacks() => RequestedKeys.ShouldBeEmpty();
}
