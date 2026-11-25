// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineOwnership;

public class when_flushing_replay_after_graph_eviction : given.an_evicted_graph_with_a_live_pipeline
{
    bool _failed;

    async Task Because() => _failed = (await _replay.FlushReplayFor(_details)).TryGetError(out _);

    [Fact] void should_flush_the_live_replay_sink() => _sink.Received(1).EndBulk();
    [Fact] void should_report_the_failed_write() => _failed.ShouldBeTrue();
    [Fact] void should_not_restore_the_graph_cache_entry() => GraphShouldRemainEvicted();
    [Fact] void should_not_compile_a_replacement_graph() => ShouldNotCompileAnotherGraph();
}
