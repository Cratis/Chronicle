// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

using ReadModelsConfiguration = Cratis.Chronicle.Configuration.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_cancelled_during_a_page : given.a_page_job_step
{
    readonly CancellationTokenSource _cancellation = new();

    protected override ReadModelsConfiguration CreateReadModelsConfig() => new() { MaxBatchSize = 2 };

    void Establish() => _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(call =>
        {
            var page = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
            _pages.Add(page);
            _cancellation.Cancel();
            return ObserverSubscriberResult.Ok(page[^1].Context.SequenceNumber);
        });

    async Task Because() => await _jobStep.InvokePerformStep(_performState, _cancellation.Token);

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_not_dispatch_the_next_chunk() => _pages.Count.ShouldEqual(1);
    [Fact] void should_report_the_handled_prefix() => _observer.Received(1).ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Is<IReadOnlyList<HandledEventsReport>>(reports => reports.Count == 2 && reports.Max(report => report.HandledUpTo) == (EventSequenceNumber)2UL));
    [Fact] void should_keep_the_last_durable_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
