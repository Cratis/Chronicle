// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_activating;

public class and_not_restricted : given.a_manager_over_several_namespaces
{
    async Task Establish() => await _manager.Add(DefinitionFor());
    async Task Because() => await _manager.OnActivateAsync(CancellationToken.None);

    [Fact] void should_observe_every_well_formed_namespace() => RequestedKeys.ShouldContainOnly(KeyFor("Default"), KeyFor("Tenant1"), KeyFor("Tenant2"));
    [Fact] void should_never_resolve_the_malformed_namespace() => RequestedKeys.ShouldNotContain(KeyFor(Malformed));
}
