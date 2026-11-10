// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

public class and_a_write_fails_for_one_partition : given.a_page_pipeline
{
    void Establish() => _sink.FailingWrites = ["third"];

    Task Because() => ReduceThePage();

    [Fact] void should_still_write_the_other_partitions() => _sink.Writes.ShouldContainOnly(Partitions);
    [Fact] void should_report_the_failed_partition() => _outcome.FailedPartitions.Single().EventSourceId.Value.ShouldEqual("third");
    [Fact] void should_report_no_change_for_the_failed_partition() => _outcome.Notifications.Keys.Select(_ => _.Value).ShouldNotContain("third");
    [Fact] void should_report_a_change_for_the_others() => _outcome.Notifications.Count.ShouldEqual(2);
}
