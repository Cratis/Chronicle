// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_an_identical_registration_follows_removal : given.a_manager_with_an_orphaned_projection
{
    ObserverRemovalResult _removal;

    void Establish() => _definitionComparer.Compare(Arg.Any<Concepts.Projections.ProjectionKey>(), _definition, _definition)
        .Returns(ProjectionDefinitionCompareResult.Same);

    async Task Because()
    {
        await _grain.Register([_definition]);
        _removal = await _grain.Remove(_definition.Identifier);
        _state.Projections.ShouldBeEmpty();
        _clock.Advance(TimeSpan.FromSeconds(5));
        await _grain.Register([_definition]);
    }

    [Fact] void should_remove_the_projection_before_reregistering() => _removal.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] void should_restore_the_definition() => _state.Projections.ShouldContainOnly(_definition);
    [Fact] void should_restore_the_projection_grain() => _projectionGrain.Received(1).SetDefinition(_definition);
}
