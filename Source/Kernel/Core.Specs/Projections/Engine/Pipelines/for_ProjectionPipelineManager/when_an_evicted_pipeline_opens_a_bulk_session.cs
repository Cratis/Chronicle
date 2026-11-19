// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_an_evicted_pipeline_opens_a_bulk_session : given.a_pipeline_manager
{
    IProjectionPipeline _pipeline;
    bool _foundBefore;
    ProjectionPipelineBinding _binding;

    async Task Establish()
    {
        _pipeline = await _manager.GetFor(_store, _namespace, _projection);
        _manager.EvictFor(_store, _namespace, _projection.Identifier);
        _foundBefore = _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _);
    }

    async Task Because()
    {
        await _pipeline.BeginBulk();
        _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _binding);
    }

    [Fact] void should_not_have_been_found_before() => _foundBefore.ShouldBeFalse();
    [Fact] void should_be_discoverable_for_flush() => _binding.Pipeline.ShouldEqual(_pipeline);
}
