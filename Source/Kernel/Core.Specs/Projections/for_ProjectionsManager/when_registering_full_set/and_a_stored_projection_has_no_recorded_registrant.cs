// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

/// <summary>
/// A projection stored before registrants were recorded cannot be attributed to any application, so no
/// application's full set may retire it.
/// </summary>
public class and_a_stored_projection_has_no_recorded_registrant : given.a_projections_manager_grain
{
    ProjectionDefinition _mine;
    ProjectionDefinition _legacy;

    void Establish()
    {
        _mine = CreateDefinition("my-projection", "my-read-model");
        _legacy = CreateDefinition("legacy-projection", "legacy-read-model");
        _state.Projections = [_mine, _legacy];
        _readModelDefinitions = [CreateReadModelDefinition("my-read-model"), CreateReadModelDefinition("legacy-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_mine], ProjectionOwner.Client, "my-app");

    [Fact] void should_keep_the_unattributed_projection() => _state.Projections.ShouldContainOnly(_mine, _legacy);
    [Fact] void should_record_the_registrant_for_the_registered_projection() => _state.Registrants["my-projection"].ShouldEqual("my-app");
}
