// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

public class and_one_partition_fails_in_the_client : given.a_page_pipeline
{
    void Establish() => _clientResults["second"] = ObserverSubscriberResult.Failed(EventSequenceNumber.First, "boom");

    Task Because() => ReduceThePage();

    [Fact] void should_write_no_state_for_the_failed_partition() => _sink.Writes.ShouldNotContain("second");
    [Fact] void should_write_the_other_partitions() => _sink.Writes.ShouldContainOnly("first", "third");
    [Fact] void should_report_no_change_for_the_failed_partition() => _outcome.Notifications.Keys.Select(_ => _.Value).ShouldNotContain("second");
    [Fact] void should_pass_on_the_result_of_the_failed_partition() => _outcome.Results.Single(_ => Equals(_.Key.Value, "second")).Value.ObserverResult.State.ShouldEqual(ObserverSubscriberState.Failed);
}
