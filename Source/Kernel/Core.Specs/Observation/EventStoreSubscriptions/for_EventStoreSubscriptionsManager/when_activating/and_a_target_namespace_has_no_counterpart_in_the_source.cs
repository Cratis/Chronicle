// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_activating;

public class and_a_target_namespace_has_no_counterpart_in_the_source : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _sourceHas.Remove("Tenant2");
        await _manager.Add(DefinitionFor());
        _requestedKeys.Clear();
    }

    async Task Because() => await _manager.OnActivateAsync(CancellationToken.None);

    [Fact] void should_observe_the_namespaces_the_source_has() => RequestedKeys.ShouldContainOnly(KeyFor("Default"), KeyFor("Tenant1"));
    [Fact] void should_never_resolve_an_observer_that_would_create_the_namespace_in_the_source() => RequestedKeys.ShouldNotContain(KeyFor("Tenant2"));
    [Fact] void should_have_asked_the_source_whether_it_has_the_namespace() => _sourceNamespacesRequested.ShouldContain("Tenant2");
}
