// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_an_old_build_finishes_after_eviction : given.a_pipeline_manager
{
    IProjectionPipeline _stale;
    IProjectionPipeline _result;
    IProjection _fresh;

    async Task Establish()
    {
        var held = new TaskCompletionSource<ISink>();
        _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.GetFor(_projection.ReadModel).Returns(held.Task, Task.FromResult(_sink));
        var oldBuild = _manager.GetFor(_store, _namespace, _projection);
        _manager.EvictFor(_store, _namespace, _projection.Identifier);
        _fresh = Substitute.For<IProjection>();
        _fresh.Identifier.Returns(_projection.Identifier);
        _fresh.EventSequenceId.Returns(_projection.EventSequenceId);
        _fresh.ReadModel.Returns(_projection.ReadModel);
        _fresh.DefinitionFingerprint.Returns("fresh");
        await _manager.GetFor(_store, _namespace, _fresh);
        held.SetResult(_sink);
        _stale = await oldBuild;
    }

    async Task Because() => _result = await _manager.GetFor(_store, _namespace, _fresh);

    [Fact] void should_not_return_the_stale_pipeline() => _result.ShouldNotBeSame(_stale);
}
