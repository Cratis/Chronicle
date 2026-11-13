// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineOwnership;

public class when_resolving_an_active_pipeline_after_graph_eviction : given.an_evicted_graph_with_a_live_pipeline
{
    ProjectionPipelineBinding _binding;

    async Task Because() => _binding = (await ProjectionPipelineLookup.GetFor(_manager, _pipelines, _details.Key))!;

    [Fact] void should_return_the_same_pipeline() => _binding.Pipeline.ShouldEqual(_livePipeline);
    [Fact] void should_return_the_graph_owned_by_that_pipeline() => _binding.Projection.ShouldEqual(_ownedGraph);
    [Fact] void should_not_restore_the_graph_cache_entry() => GraphShouldRemainEvicted();
    [Fact] void should_not_compile_a_replacement_graph() => ShouldNotCompileAnotherGraph();
}
