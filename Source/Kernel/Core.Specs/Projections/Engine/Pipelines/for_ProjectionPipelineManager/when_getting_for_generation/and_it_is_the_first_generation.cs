// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_getting_for_generation;

public class and_it_is_the_first_generation : a_generation_pipeline_manager
{
    IProjectionPipeline _primary;
    IProjectionPipeline _first;

    async Task Establish() => _primary = await _manager.GetFor(_store, _namespace, _projection);

    async Task Because() => _first = await _manager.GetFor(_store, _namespace, _projection, MaterializationGenerationIdentity.For("projection", MaterializationGeneration.First, "Model"));

    [Fact] void should_share_the_pipeline_with_the_projection() => _first.ShouldBeSame(_primary);
}
