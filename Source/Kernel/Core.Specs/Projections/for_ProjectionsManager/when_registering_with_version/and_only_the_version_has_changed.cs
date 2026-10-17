// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_only_the_version_has_changed : given.a_projections_manager_grain
{
    void Establish()
    {
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { Version = "1.0.0" }];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([CreateDefinition("the-projection", "the-read-model")], version: "1.1.0");

    [Fact] void should_persist_the_new_version() => _state.Projections.Single().Version.ShouldEqual("1.1.0");
    [Fact] async Task should_write_the_manager_state() => await _stateStorage.Received(1).WriteStateAsync();
    [Fact] async Task should_update_the_projection_grain_with_the_version() => await _projectionGrain.Received(1).SetDefinition(Arg.Is<ProjectionDefinition>(definition => definition.Version == "1.1.0"));
}
