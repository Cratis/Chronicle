// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineOwnership;

public class when_ending_catchup_bulk_after_graph_eviction : given.an_evicted_graph_with_a_live_pipeline
{
    IEnumerable<FailedPartition> _result;

    async Task Because() => _result = await _catchup.EndBulkFor(_details, _deadline);

    [Fact] void should_end_the_live_bulk_window_with_its_deadline() => _sink.Received(1).EndBulk(_deadline);
    [Fact] void should_return_the_write_failure() => _result.ShouldEqual([_failure]);
    [Fact] void should_not_restore_the_graph_cache_entry() => GraphShouldRemainEvicted();
    [Fact] void should_not_compile_a_replacement_graph() => ShouldNotCompileAnotherGraph();
}
