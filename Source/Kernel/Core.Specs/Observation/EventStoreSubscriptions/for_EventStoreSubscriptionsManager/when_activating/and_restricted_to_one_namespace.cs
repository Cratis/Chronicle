// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_activating;

public class and_restricted_to_one_namespace : given.a_manager_over_several_namespaces
{
    async Task Establish() => await _manager.Add(DefinitionFor("default"));
    async Task Because() => await _manager.OnActivateAsync(CancellationToken.None);

    [Fact] void should_only_resolve_the_observer_of_the_listed_namespace() => RequestedKeys.ShouldContainOnly(KeyFor("Default"));
    [Fact] void should_subscribe_the_listed_namespace() => WasSubscribed("Default").ShouldBeTrue();
}
