// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_the_definition_changes : given.a_pipeline_manager
{
    IProjectionPipeline _previous;
    IProjectionPipeline _current;
    readonly ReplayContext _replay = new(new("model", 1), "replay", "revert", DateTimeOffset.UtcNow);

    async Task Establish()
    {
        _previous = await _manager.GetFor(_store, _namespace, _projection);
        await _previous.ResumeReplay(_replay);
        _projection.DefinitionFingerprint.Returns("second");
    }

    async Task Because() => _current = await _manager.GetFor(_store, _namespace, _projection);

    [Fact] void should_replace_the_pipeline() => _current.ShouldNotBeSame(_previous);
    [Fact] void should_preserve_the_shared_handle_lock() => HandleLock(_current).ShouldBeSame(HandleLock(_previous));
    [Fact] async Task should_not_restart_the_replay_sink() => await _sink.Received(1).ResumeReplay(_replay);

    static object HandleLock(IProjectionPipeline pipeline) => typeof(ProjectionPipeline).GetField("_handleLock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pipeline)!;
}
