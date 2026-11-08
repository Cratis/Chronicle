// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

public class and_every_partition_succeeds : given.a_page_pipeline
{
    void Establish() => _sink.States["second"] = StateWith(2);

    Task Because() => ReduceThePage();

    [Fact] void should_call_the_client_once_for_the_whole_page() => _reducerCalls.Count.ShouldEqual(1);
    [Fact] void should_give_the_client_every_partition() => _reducerCalls[0].Select(_ => _.Key.Value).ShouldContainOnly(Partitions);
    [Fact] void should_give_the_client_the_stored_state_of_each_partition() => _reducerCalls[0].Single(_ => Equals(_.Key.Value, "second")).InitialState.ShouldEqual(_sink.States["second"]);
    [Fact] void should_give_the_client_no_state_for_a_partition_without_a_read_model() => _reducerCalls[0].Single(_ => Equals(_.Key.Value, "first")).InitialState.ShouldBeNull();
    [Fact] void should_prefetch_the_states_of_every_partition_in_one_read() => _sink.Prefetches.Single().Select(_ => _.Value).ShouldContainOnly(Partitions);
    [Fact] void should_prefetch_before_reading_any_state() => _sink.Calls.IndexOf("PrefetchPage").ShouldBeLessThan(_sink.Calls.FindIndex(_ => _.StartsWith("FindOrDefault")));
    [Fact] void should_write_every_partition() => _sink.Writes.ShouldContainOnly(Partitions);
    [Fact] void should_open_one_bulk_window() => _sink.Calls.Count(_ => _ == "BeginBulk").ShouldEqual(1);
    [Fact] void should_close_the_bulk_window_after_every_write() => _sink.Calls[^1].ShouldEqual("EndBulk");
    [Fact] void should_write_within_the_bulk_window() => _sink.Calls.IndexOf("BeginBulk").ShouldBeLessThan(_sink.Calls.FindIndex(_ => _.StartsWith("ApplyChanges")));
    [Fact] void should_report_a_change_for_every_partition() => _outcome.Notifications.Count.ShouldEqual(3);
    [Fact] void should_report_no_failed_partitions() => _outcome.FailedPartitions.ShouldBeEmpty();
}
