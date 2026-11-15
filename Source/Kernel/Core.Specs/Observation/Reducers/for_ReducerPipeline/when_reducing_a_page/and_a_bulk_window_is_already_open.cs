// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

/// <summary>
/// A replay holds the sink's bulk window for its whole walk; the page joins it rather than replacing it.
/// </summary>
public class and_a_bulk_window_is_already_open : given.a_page_pipeline
{
    void Establish() => _sink.IsInBulkMode = true;

    Task Because() => ReduceThePage();

    [Fact] void should_not_begin_a_window() => _sink.Calls.ShouldNotContain("BeginBulk");
    [Fact] void should_not_end_the_open_window() => _sink.Calls.ShouldNotContain("EndBulk");
    [Fact] void should_write_every_partition() => _sink.Writes.ShouldContainOnly(Partitions);
    [Fact] void should_still_prefetch_the_page() => _sink.Prefetches.Count.ShouldEqual(1);
}
