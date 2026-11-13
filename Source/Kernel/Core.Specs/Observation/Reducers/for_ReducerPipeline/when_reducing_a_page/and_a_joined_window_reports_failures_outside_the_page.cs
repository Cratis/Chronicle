// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

/// <summary>
/// A write that joins an open window can report the failures of a shared flush, including partitions outside the page.
/// </summary>
public class and_a_joined_window_reports_failures_outside_the_page : given.a_page_pipeline
{
    void Establish()
    {
        _sink.IsInBulkMode = true;
        _sink.FailingWrites = ["second"];
        _sink.FailuresReportedByWrite["second"] = [new FailedPartition((Key)"outside-the-page", 7UL)];
    }

    Task Because() => ReduceThePage();

    [Fact] void should_report_every_failure_the_window_reported() => _outcome.FailedPartitions.Select(_ => _.EventSourceId.Value).ShouldContainOnly("second", "outside-the-page");
    [Fact] void should_report_no_change_for_the_partition_whose_write_was_lost() => _outcome.Notifications.Keys.Select(_ => _.Value).ShouldNotContain("second");
    [Fact] void should_not_end_the_open_window() => _sink.Calls.ShouldNotContain("EndBulk");
}
