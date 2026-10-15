// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

/// <summary>
/// A full set from an application still retires the projections that same application registered earlier and
/// no longer includes.
/// </summary>
public class and_the_same_application_registered_the_stored_projection : given.a_projections_manager_grain
{
    ProjectionDefinition _remaining;
    ProjectionDefinition _orphan;
    ProjectionDefinition _theirs;

    void Establish()
    {
        _remaining = CreateDefinition("remaining-projection", "remaining-read-model");
        _orphan = CreateDefinition("orphaned-projection", "orphaned-read-model");
        _theirs = CreateDefinition("their-projection", "their-read-model");
        _state.Projections = [_remaining, _orphan, _theirs];
        _state.Registrants = new Dictionary<string, string>
        {
            ["remaining-projection"] = "my-app",
            ["orphaned-projection"] = "my-app",
            ["their-projection"] = "their-app"
        };
        _readModelDefinitions =
        [
            CreateReadModelDefinition("remaining-read-model", "remainingContainer"),
            CreateReadModelDefinition("orphaned-read-model", "orphanedContainer"),
            CreateReadModelDefinition("their-read-model", "theirContainer")
        ];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_remaining], ProjectionOwner.Client, "my-app");

    [Fact] async Task should_retire_only_the_orphaned_observer() => await _observerGrain.Received(1).Retire();
    [Fact] void should_keep_the_remaining_and_the_other_applications_projections() => _state.Projections.ShouldContainOnly(_remaining, _theirs);
}
