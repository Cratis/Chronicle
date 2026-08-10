// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_only_some_definitions_have_changed : given.a_projections_manager_grain
{
    ProjectionDefinition _unchangedExisting;
    ProjectionDefinition _unchangedIncoming;
    ProjectionDefinition _changedExisting;
    ProjectionDefinition _changedIncoming;

    void Establish()
    {
        _unchangedExisting = CreateDefinition("unchanged-projection", "the-read-model");
        _unchangedIncoming = CreateDefinition("unchanged-projection", "the-read-model");
        _changedExisting = CreateDefinition("changed-projection", "the-read-model");
        _changedIncoming = _changedExisting with { IsRewindable = false };
        _state.Projections = [_unchangedExisting, _changedExisting];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), _unchangedExisting, _unchangedIncoming)
            .Returns(ProjectionDefinitionCompareResult.Same);
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), _changedExisting, _changedIncoming)
            .Returns(ProjectionDefinitionCompareResult.Different);
    }

    async Task Because() => await _grain.Register([_unchangedIncoming, _changedIncoming]);

    [Fact]
    void should_only_register_the_changed_definition_with_the_engine() =>
        _projectionsServiceClient.Received(1).Register(
            (EventStoreName)EventStore,
            Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.SequenceEqual(new[] { _changedIncoming })));

    [Fact] void should_only_set_the_changed_definition_on_its_projection_grain() => _projectionGrain.Received(1).SetDefinition(_changedIncoming);

    [Fact]
    void should_keep_the_unchanged_definition_and_replace_the_changed_one() =>
        _state.Projections.ShouldContainOnly(_unchangedExisting, _changedIncoming);
}
