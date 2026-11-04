// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_creating_an_isolated_pipeline;

public class twice : given.a_pipeline_manager
{
    IProjectionPipeline _first;
    IProjectionPipeline _second;

    void Because()
    {
        _first = _manager.CreateIsolated(_store, _namespace, _projection, Substitute.For<ISink>());
        _second = _manager.CreateIsolated(_store, _namespace, _projection, Substitute.For<ISink>());
    }

    [Fact] void should_not_cache_the_pipeline() => _second.ShouldNotBeSame(_first);
}
