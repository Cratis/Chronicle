// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_the_definition_is_unchanged : given.a_pipeline_manager
{
    IProjectionPipeline _previous;
    IProjectionPipeline _current;

    async Task Establish() => _previous = await _manager.GetFor(_store, _namespace, _projection);
    async Task Because() => _current = await _manager.GetFor(_store, _namespace, _projection);

    [Fact] void should_reuse_the_pipeline() => _current.ShouldBeSame(_previous);
}
