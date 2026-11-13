// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page.and_the_sink_writes_pages;

/// <summary>
/// A replay holds the sink's bulk window; the page joins it rather than writing past it.
/// </summary>
public class and_a_bulk_window_is_already_open : given.a_page_writing_sink
{
    void Establish() => _sink.IsInBulkMode = true;

    Task Because() => ReduceThePage();

    [Fact] void should_not_write_past_the_window() => PageWritingSink.PageWrites.ShouldBeEmpty();
    [Fact] void should_write_every_partition_into_the_window() => _sink.Buffered.ShouldContainOnly(Partitions);
    [Fact] void should_not_end_the_open_window() => _sink.Calls.ShouldNotContain("EndBulk");
}
