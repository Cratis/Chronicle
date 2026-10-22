// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_a_partition_fails_mid_page : given.a_performing_job_step
{
    IReadOnlyList<HandledEventsReport> _reports;
    bool _reportedBeforeFailure;

    void Establish()
    {
        _eventCursor.Current.Returns([CreateEvent(1UL, "module"), CreateEvent(2UL, "feature"), CreateEvent(3UL, "feature"), CreateEvent(4UL, "module")]);
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var partition = call.Arg<Key>();
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                _handledBatches.Add(new(partition, events.Select(e => e.Context.SequenceNumber).ToArray()));
                return Task.FromResult(partition == (Key)"feature"
                    ? ObserverSubscriberResult.Failed(2UL, "boom")
                    : ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
        _observer.ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>()).Returns(call =>
        {
            _reports = call.ArgAt<IReadOnlyList<HandledEventsReport>>(1);
            return Task.CompletedTask;
        });
        _observer.When(o => o.PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>()))
            .Do(_ => _reportedBeforeFailure = _reports is not null);
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_flush_the_successful_prefix_before_reporting_failure() => _reportedBeforeFailure.ShouldBeTrue();
    [Fact] void should_count_only_the_successful_part_of_the_failed_run() => _reports.Single(r => r.Partition == (Key)"feature").Counts.Values.Single().ShouldEqual((EventCount)1UL);
    [Fact] void should_record_the_partial_partition_tail() => _reports.Single(r => r.Partition == (Key)"feature").HandledUpTo.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_report_the_same_failure_position() => _observer.Received(1).PartitionFailed((Key)"feature", (EventSequenceNumber)3UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling);
    [Fact] void should_stop_at_the_first_failure() => _handledBatches.Count.ShouldEqual(2);
}
