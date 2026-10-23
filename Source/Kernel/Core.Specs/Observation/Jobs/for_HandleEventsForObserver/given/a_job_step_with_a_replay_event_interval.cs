// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

/// <summary>
/// A performing job step whose checkpoint debounce never fires, so only the replay event interval can checkpoint mid-page.
/// </summary>
public class a_job_step_with_a_replay_event_interval : a_performing_job_step
{
    protected readonly List<bool> _flushes = [];
    protected readonly List<IReadOnlyList<HandledEventsReport>> _reports = [];
    protected readonly List<EventSequenceNumber> _durableAtReport = [];

    protected virtual int ReplayCheckpointEventInterval => 2;

    protected override Cratis.Chronicle.Configuration.Jobs CreateJobsConfig() => new()
    {
        ReplayCheckpointEventInterval = ReplayCheckpointEventInterval,
        StepCheckpointBatchInterval = 100,
        StepCheckpointFlushInterval = TimeSpan.Zero
    };

    void Establish()
    {
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>())
            .Returns(call =>
            {
                _flushes.Add(call.ArgAt<bool>(1));
                return Task.FromResult<IReadOnlyDictionary<SiloAddress, Guid[]>>(new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = [] });
            });
        _observer.ReportHandledEventsBatch(Arg.Any<JobStepId>(), Arg.Any<IReadOnlyList<HandledEventsReport>>()).Returns(call =>
        {
            _reports.Add(call.ArgAt<IReadOnlyList<HandledEventsReport>>(1));
            _durableAtReport.Add(_performState.LastSuccessfullyHandledEventSequenceNumber);
            return Task.CompletedTask;
        });
    }

    protected void SetupPages(params AppendedEvent[][] pages)
    {
        var page = 0;
        _eventCursor.Current.Returns(pages[0], pages.Skip(1).ToArray());
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(page++ < pages.Length));
    }

    protected AppendedEvent[] AlternatingEvents(ulong from, ulong to) =>
        [.. Enumerable.Range((int)from, (int)(to - from + 1)).Select(n => CreateEvent((ulong)n, n % 2 == 0 ? "even" : "odd"))];
}
