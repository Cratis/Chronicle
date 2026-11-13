// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page.and_the_sink_writes_pages;

public class and_the_page_write_throws : given.a_page_writing_sink
{
    void Establish() => PageWritingSink.PageWriteError = new PageWriteOutcomeUnknown();

    Task Because() => ReduceThePage();

    [Fact] void should_fail_every_partition_it_wrote() => _outcome.FailedPartitions.Select(_ => _.EventSourceId.Value).ShouldContainOnly(Partitions);
    [Fact] void should_report_the_outcome_as_unknown() => _outcome.FailedPartitions.All(_ => _.OutcomeUnknown).ShouldBeTrue();
    [Fact] void should_report_no_changes() => _outcome.Notifications.ShouldBeEmpty();

    sealed class PageWriteOutcomeUnknown() : Exception("The page write outcome is unknown");
}
