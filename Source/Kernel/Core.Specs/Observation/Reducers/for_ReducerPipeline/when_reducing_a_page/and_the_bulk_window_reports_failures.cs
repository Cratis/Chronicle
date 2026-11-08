// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

public class and_the_bulk_window_reports_failures : given.a_page_pipeline
{
    void Establish() => _sink.FailuresAtEndOfBulk = [new FailedPartition((Key)"second", 1UL), new FailedPartition((Key)"outside-the-page", 7UL)];

    Task Because() => ReduceThePage();

    [Fact] void should_report_every_failure_the_window_reported() => _outcome.FailedPartitions.Select(_ => _.EventSourceId.Value).ShouldContainOnly("second", "outside-the-page");
    [Fact] void should_report_no_change_for_the_partition_whose_write_was_lost() => _outcome.Notifications.Keys.Select(_ => _.Value).ShouldNotContain("second");
}
