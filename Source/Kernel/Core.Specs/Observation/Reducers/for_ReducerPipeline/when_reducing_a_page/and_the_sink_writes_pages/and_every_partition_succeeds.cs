// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page.and_the_sink_writes_pages;

public class and_every_partition_succeeds : given.a_page_writing_sink
{
    void Establish() => _sink.States["second"] = StateWith(2);

    Task Because() => ReduceThePage();

    [Fact] void should_read_every_partition_in_one_page_read() => PageWritingSink.PageReads.Single().Select(_ => _.Value).ShouldContainOnly(Partitions);
    [Fact] void should_not_read_any_partition_on_its_own() => _sink.Calls.ShouldNotContain(_ => _.StartsWith("FindOrDefault"));
    [Fact] void should_give_the_client_the_stored_state() => _reducerCalls[0].Single(_ => Equals(_.Key.Value, "second")).InitialState.ShouldEqual(_sink.States["second"]);
    [Fact] void should_write_every_partition_in_one_page_write() => PageWritingSink.PageWrites.Single().ShouldContainOnly(Partitions);
    [Fact] void should_not_write_any_partition_on_its_own() => _sink.Calls.ShouldNotContain(_ => _.StartsWith("ApplyChanges"));
    [Fact] void should_not_begin_a_bulk_window() => _sink.Calls.ShouldNotContain("BeginBulk");
    [Fact] void should_not_end_a_bulk_window() => _sink.Calls.ShouldNotContain("EndBulk");
    [Fact] void should_report_a_change_for_every_partition() => _outcome.Notifications.Count.ShouldEqual(3);
    [Fact] void should_report_no_failed_partitions() => _outcome.FailedPartitions.ShouldBeEmpty();
}
