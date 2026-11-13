// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineOwnership;

public class when_resuming_and_finishing_catchup_after_graph_eviction : given.an_evicted_graph_with_a_live_pipeline
{
    async Task Because()
    {
        await _catchup.ResumeCatchupFor(_details);
        await _catchup.EndCatchupFor(_details);
    }

    [Fact] void should_resume_the_live_bulk_window() => _sink.Received(1).ResumeBulk();
    [Fact] void should_finish_the_live_bulk_window() => _sink.Received(1).EndBulk();
    [Fact] void should_not_restore_the_graph_cache_entry() => GraphShouldRemainEvicted();
    [Fact] void should_not_compile_a_replacement_graph() => ShouldNotCompileAnotherGraph();
}
