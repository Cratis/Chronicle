// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy.given;

/// <summary>
/// A client reactor observer that was released as dormant: its state and record are stored, its activation is gone.
/// </summary>
public class a_dormant_client_observer : when_going_dormant.given.a_subscribed_client_observer
{
    protected List<AppendedEvent> _log = [];
    protected List<(Key Partition, AppendedEvent[] Events)> _delivered = [];
    protected EventSequenceNumber _tail = EventSequenceNumber.Unavailable;
    protected ObserverState _stored;

    protected virtual bool FloorIsReachedWhenWaking => true;

    protected virtual int ResumeMaxEvents => 1000;

    protected override Observers CreateObserversConfig() => new()
    {
        WriteCoalescingInterval = 0,
        IdleDeactivationAfter = 0,
        DormantAfter = DormantAfter,
        DormantResumeMaxEvents = ResumeMaxEvents,
        DurableSubscriptions = Mode
    };

    protected static AppendedEvent EventAt(ulong sequenceNumber, string partition = "partition") =>
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, sequenceNumber) with
        {
            Context = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, sequenceNumber).Context with
            {
                EventSourceId = partition
            }
        };

    /// <summary>
    /// Gets a chance to register more clients before the observer goes dormant.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task BeforeGoingDormant() => Task.CompletedTask;

    async Task Establish()
    {
        await BeforeGoingDormant();
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
        await Deactivate();
        _stored = _stateStorage.State;
    }

    /// <summary>
    /// Appends events to the log the woken observer reads; the tail follows them.
    /// </summary>
    /// <param name="events">The events appended while the observer was dormant.</param>
    protected void Append(params AppendedEvent[] events)
    {
        _log.AddRange(events);
        _tail = _log.Max(_ => _.Context.SequenceNumber);
    }

    /// <summary>
    /// Activates a new observer from what the dormant one left in storage, the way an event or the safety net does.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected async Task Wake()
    {
        _reactor = Substitute.For<IReactorObserverSubscriber>();
        _reactor.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(callInfo =>
        {
            var events = callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray();
            _delivered.Add((callInfo.Arg<Key>(), events));
            return ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber);
        });

        _eventSequence.GetTailSequenceNumber().Returns(_ => _tail);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(callInfo =>
        {
            var from = callInfo.Arg<EventSequenceNumber>();
            var next = _log.Where(_ => _.Context.SequenceNumber >= from).OrderBy(_ => _.Context.SequenceNumber).FirstOrDefault();
            return next is null ? EventSequenceNumber.Unavailable : next.Context.SequenceNumber;
        });
        _eventSequenceStorage
            .GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var start = callInfo.ArgAt<EventSequenceNumber>(0);
                var end = callInfo.ArgAt<EventSequenceNumber>(1);
                return Task.FromResult<IEventCursor>(new ListEventCursor(_log.Where(_ => _.Context.SequenceNumber >= start && _.Context.SequenceNumber <= end).OrderBy(_ => _.Context.SequenceNumber)));
            });

        _clusterReleases.Summarize().Returns(FloorIsReachedWhenWaking
            ? new Releases.ClusterReleaseSummary([(new Concepts.System.SemanticVersion(19, 173, 0), 1)], 0, 0)
            : new Releases.ClusterReleaseSummary([(new Concepts.System.SemanticVersion(19, 172, 0), 1), (new Concepts.System.SemanticVersion(19, 173, 0), 1)], 0, 0));
        _appendedEventsQueues.ClearReceivedCalls();
        _appendedEventsQueues
            .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>())
            .Returns(callInfo => new EventSequences.AppendedEventsQueueSubscription(callInfo.Arg<ObserverKey>(), 0));
        await Crash();
    }

    /// <summary>
    /// Wakes the observer and has a second activation of it, which is what Orleans does when the lane that rang the bell delivers.
    /// </summary>
    /// <param name="partition">The partition of the events.</param>
    /// <param name="events">The events the doorbell carries.</param>
    /// <returns>Awaitable task.</returns>
    protected Task RingDoorbell(string partition, params AppendedEvent[] events) => _observer.Handle(partition, events);
}
