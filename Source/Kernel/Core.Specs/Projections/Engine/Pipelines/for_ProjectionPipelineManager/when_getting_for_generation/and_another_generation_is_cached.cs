// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_getting_for_generation;

public class and_another_generation_is_cached : a_generation_pipeline_manager
{
    IProjectionPipeline _primary;
    IProjectionPipeline _generation;
    readonly ReplayContext _replay = new(new("model", 1), "Model", "revert", DateTimeOffset.UtcNow);

    async Task Establish() => _primary = await _manager.GetFor(_store, _namespace, _projection);

    async Task Because()
    {
        _generation = await _manager.GetFor(_store, _namespace, _projection, _second);
        await _generation.BeginReplay(_replay);
        await _generation.EndReplay(_replay);
    }

    [Fact] void should_build_a_separate_pipeline() => _generation.ShouldNotBeSame(_primary);
    [Fact] void should_not_share_the_handle_lock() => HandleLock(_generation).ShouldNotBeSame(HandleLock(_primary));
    [Fact] async Task should_address_the_generations_destination() => await _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.Received(1).GetFor(_projection.ReadModel, _second.Destination);
    [Fact] void should_record_changesets_under_the_generations_destination() => ((ReadModelContainerName)typeof(ProjectionPipeline).GetProperty("ChangesetContainer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(_generation)!).ShouldEqual(new ReadModelContainerName("Model-g2"));
    [Fact] void should_not_put_the_generations_sink_in_replay_mode() => _generationSink.DidNotReceive().BeginReplay(Arg.Any<ReplayContext>());
    [Fact] void should_not_end_replay_on_the_generations_sink() => _generationSink.DidNotReceive().EndReplay(Arg.Any<ReplayContext>());
    [Fact] void should_not_touch_the_primary_sink() => _sink.DidNotReceive().BeginReplay(Arg.Any<ReplayContext>());
    [Fact] void should_key_futures_by_the_generations_observer() => _grainFactory.Received(1).GetGrain<IProjectionFutures>(new Concepts.Projections.ProjectionFuturesKey("projection@g2", _store, _namespace).ToString(), null);
}
