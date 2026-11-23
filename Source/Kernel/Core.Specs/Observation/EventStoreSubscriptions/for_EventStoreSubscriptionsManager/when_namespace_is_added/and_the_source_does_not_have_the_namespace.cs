// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_namespace_is_added;

public class and_the_source_does_not_have_the_namespace : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        await _manager.Add(DefinitionFor());
        await AttachNamespaceAdded();
    }

    async Task Because() => await _onNamespaceAdded(new("Target", "Tenant9"));

    [Fact] void should_not_subscribe_the_namespace() => WasSubscribed("Tenant9").ShouldBeFalse();
    [Fact] void should_never_resolve_an_observer_that_would_create_the_namespace_in_the_source() => RequestedKeys.ShouldNotContain(KeyFor("Tenant9"));
    [Fact] void should_have_asked_the_source_whether_it_has_the_namespace() => _sourceNamespacesRequested.ShouldContain("Tenant9");
}
