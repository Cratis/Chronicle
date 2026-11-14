// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_activating;

public class and_a_restriction_was_never_enforced : given.a_manager_over_several_namespaces
{
    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor("Default"));
        _observers.Clear();
        _requestedKeys.Clear();
    }

    async Task Because() => await _manager.OnActivateAsync(CancellationToken.None);

    [Fact] void should_revoke_the_observers_outside_the_list_after_a_restart() => _observers[KeyFor("Tenant1")].Received(1).Unsubscribe();
}
