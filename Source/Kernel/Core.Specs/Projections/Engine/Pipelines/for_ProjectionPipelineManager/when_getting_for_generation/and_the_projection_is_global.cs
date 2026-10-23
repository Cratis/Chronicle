// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Generations;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_getting_for_generation;

public class and_the_projection_is_global : a_generation_pipeline_manager
{
    Exception _error;

    void Establish() => _projection.Scope.Returns(ProjectionScope.Global);

    async Task Because() => _error = await Catch.Exception(() => _manager.GetFor(_store, _namespace, _projection, _second));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionCannotBuildGenerations>();
    [Fact] async Task should_not_create_a_sink() => await _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.DidNotReceive().GetFor(Arg.Any<Concepts.ReadModels.ReadModelDefinition>(), Arg.Any<Concepts.ReadModels.ReadModelContainerName>());
}
