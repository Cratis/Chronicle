// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

using ReadModelsConfiguration = Cratis.Chronicle.Configuration.ReadModels;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

public class and_stopped_during_a_page : given.a_page_job_step
{
    protected override ReadModelsConfiguration CreateReadModelsConfig() => new() { MaxBatchSize = 2 };

    void Establish() => _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(call =>
        {
            var page = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
            _pages.Add(page);
            _jobStep.Run.Close();
            return ObserverSubscriberResult.Ok(page[^1].Context.SequenceNumber);
        });

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_dispatch_the_next_chunk() => _pages.Count.ShouldEqual(1);
    [Fact] void should_not_report_rejected_work() => _observer.DidNotReceive().ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>());
    [Fact] void should_not_advance_the_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
