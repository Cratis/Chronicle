// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

/// <summary>
/// Several applications share one event store and each registers its own full set. A full set from one of them
/// must not retire the projections another application registered, which is not part of its set.
/// </summary>
public class and_another_application_registered_the_stored_projection : given.a_projections_manager_grain
{
    ProjectionDefinition _mine;
    ProjectionDefinition _theirs;

    void Establish()
    {
        _mine = CreateDefinition("my-projection", "my-read-model");
        _theirs = CreateDefinition("their-projection", "their-read-model");
        _state.Projections = [_mine, _theirs];
        _state.Registrants = new Dictionary<string, string>
        {
            ["my-projection"] = "my-app",
            ["their-projection"] = "their-app"
        };
        _readModelDefinitions = [CreateReadModelDefinition("my-read-model"), CreateReadModelDefinition("their-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_mine], ProjectionOwner.Client, "my-app");

    [Fact] void should_keep_the_other_applications_projection() => _state.Projections.ShouldContainOnly(_mine, _theirs);
    [Fact] async Task should_not_retire_any_observer() => await _observerGrain.DidNotReceive().Retire();
    [Fact] void should_not_remove_any_projection_grain() => _projectionGrain.DidNotReceive().Remove();
}
