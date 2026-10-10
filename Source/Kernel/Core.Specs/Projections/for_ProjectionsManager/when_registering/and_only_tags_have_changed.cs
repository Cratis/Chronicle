// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_only_tags_have_changed : given.a_projections_manager_grain
{
    ProjectionDefinition _incoming;

    void Establish()
    {
        var existing = CreateDefinition("the-projection", "the-read-model") with { Tags = ["Orders"] };
        _incoming = existing with { Tags = ["Analytics"] };
        _state.Projections = [existing];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_incoming]);

    [Fact] void should_persist_the_updated_tags() => _state.Projections.Single().Tags!.ShouldContainOnly("Analytics");
    [Fact] async Task should_write_the_manager_state() => await _stateStorage.Received(1).WriteStateAsync();
    [Fact] async Task should_update_the_projection_grain() => await _projectionGrain.Received(1).SetDefinition(_incoming);
    [Fact] async Task should_update_the_engine_definition() => await _projectionsServiceClient.Received(1).Register((EventStoreName)EventStore, Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Single().Tags!.SequenceEqual(new[] { "Analytics" })));
}
