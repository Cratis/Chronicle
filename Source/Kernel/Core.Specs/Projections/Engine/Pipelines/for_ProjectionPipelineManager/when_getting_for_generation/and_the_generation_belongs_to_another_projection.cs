// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Projections.Generations;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_getting_for_generation;

public class and_the_generation_belongs_to_another_projection : a_generation_pipeline_manager
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _manager.GetFor(_store, _namespace, _projection, MaterializationGenerationIdentity.For("other", 2U, "Model")));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GenerationBelongsToAnotherProjection>();
}
