// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_bounded_replay_job_step : a_page_job_step
{
    protected readonly List<AppendedEvent[]> _dispatches = [];
    protected readonly List<EventSequenceNumber> _durableAtDispatch = [];
    protected readonly List<EventSequenceNumber> _durableAtReport = [];
    protected readonly List<EventSequenceNumber> _frontiersAtReport = [];
    protected readonly List<HandledEventsReport> _reports = [];

    protected virtual int Interval => 3;
    protected virtual int PageCap => 100;
    protected virtual bool UsesPages => true;

    protected override Cratis.Chronicle.Configuration.Jobs CreateJobsConfig() => new()
    {
        ReplayCheckpointEventInterval = Interval,
        StepCheckpointBatchInterval = 100,
        StepCheckpointFlushInterval = TimeSpan.Zero
    };

    protected override Cratis.Chronicle.Configuration.ReadModels CreateReadModelsConfig() => new() { MaxBatchSize = PageCap };

    void Establish()
    {
        if (!UsesPages)
        {
            _observer.GetSubscription().Returns(new ObserverSubscription("observer-id", _performState.ObserverKey, [], typeof(ISomeObserverType), SiloAddress.Zero));
        }

        _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => Observe(call.Arg<IEnumerable<AppendedEvent>>()));
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => Observe(call.ArgAt<IEnumerable<AppendedEvent>>(1)));
        _observer.ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>()).Returns(call =>
        {
            var reports = call.ArgAt<IReadOnlyList<HandledEventsReport>>(1);
            _reports.AddRange(reports);
            _frontiersAtReport.Add(reports.Max(report => report.HandledUpTo));
            _durableAtReport.Add(_performState.LastSuccessfullyHandledEventSequenceNumber);
            return Task.CompletedTask;
        });
    }

    protected ObserverSubscriberResult Observe(IEnumerable<AppendedEvent> events)
    {
        var dispatched = events.ToArray();
        _dispatches.Add(dispatched);
        _durableAtDispatch.Add(_performState.LastSuccessfullyHandledEventSequenceNumber);
        return ObserverSubscriberResult.Ok(dispatched[^1].Context.SequenceNumber);
    }

    protected void SetupPages(params AppendedEvent[][] pages)
    {
        var page = 0;
        _eventCursor.Current.Returns(pages[0], pages.Skip(1).ToArray());
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(page++ < pages.Length));
    }

    protected AppendedEvent[] Events(params ulong[] positions) => positions.Select(position => CreateEvent(position, "source")).ToArray();
}
