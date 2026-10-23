// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_events_span_multiple_pages : given.a_performing_job_step
{
    readonly List<IReadOnlyList<HandledEventsReport>> _reports = [];

    void Establish()
    {
        _eventCursor.Current.Returns([CreateEvent(1UL, "module"), CreateEvent(2UL, "feature")], [CreateEvent(3UL, "module")]);
        var page = 0;
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(page++ < 2));
        _observer.ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>()).Returns(call =>
        {
            _reports.Add(call.ArgAt<IReadOnlyList<HandledEventsReport>>(1));
            return Task.CompletedTask;
        });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_send_one_batch_per_page() => _reports.Count.ShouldEqual(2);
    [Fact] void should_not_carry_counts_into_the_next_page() => _reports[1].Single().Counts.Values.Single().ShouldEqual((EventCount)1UL);
    [Fact] void should_report_the_next_page_partition_tail() => _reports[1].Single().HandledUpTo.ShouldEqual((EventSequenceNumber)3UL);
}
