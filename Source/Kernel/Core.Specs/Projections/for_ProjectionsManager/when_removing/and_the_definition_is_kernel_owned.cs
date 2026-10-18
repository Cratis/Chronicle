// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_removing;

public class and_the_definition_is_kernel_owned : given.a_manager_with_an_orphaned_projection
{
    ObserverRemovalResult _result;

    void Establish() => _state.Projections = [_definition with { Owner = ProjectionOwner.Kernel }];
    async Task Because() => _result = await _grain.Remove(_definition.Identifier);

    [Fact] void should_refuse_removal() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.ObserverActive);
    [Fact] async Task should_not_retire_the_observer() => await _observerGrain.DidNotReceive().Retire();
    [Fact] async Task should_not_delete_the_definition() => await _eventStoreStorage.Projections.DidNotReceive().Delete(_definition.Identifier);
}
