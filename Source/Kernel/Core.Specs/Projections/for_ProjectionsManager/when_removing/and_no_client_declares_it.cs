// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_removing;

public class and_no_client_declares_it : given.a_manager_with_an_orphaned_projection
{
    ObserverRemovalResult _result;

    async Task Because() => _result = await _grain.Remove(_definition.Identifier);

    [Fact] void should_remove_the_orphan() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] void should_forget_the_definition() => _state.Projections.ShouldBeEmpty();
    [Fact] async Task should_retire_in_every_namespace() => await _observerGrain.Received(2).Retire();
    [Fact] async Task should_remove_in_every_namespace() => await _observerGrain.Received(2).Remove();
    [Fact] async Task should_delete_the_observer_definition() => await _eventStoreStorage.Observers.Received(1).Delete(_definition.Identifier);
    [Fact] async Task should_delete_the_projection_definition() => await _eventStoreStorage.Projections.Received(1).Delete(_definition.Identifier);
    [Fact] async Task should_remove_the_projection_grain() => await _projectionGrain.Received(1).Remove();
    [Fact] async Task should_unregister_from_the_engine() => await _projectionsServiceClient.Received(1).Unregister(EventStore, _definition.Identifier);
}
