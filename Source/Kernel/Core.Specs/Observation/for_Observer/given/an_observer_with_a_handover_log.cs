// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_handover_log : an_observer_with_skip_detection
{
    readonly reloadable_observer_state_storage _writes = new();
    readonly List<AppendedEvent> _log = [];
    protected readonly List<EventSequenceNumber> _delivered = [];
    protected readonly List<CatchUpObserverPartitionRequest> _repairs = [];
    protected CatchUpObserverRequest? _wide;

    /// <summary>
    /// Keeps the last written state distinct from observer state that has not been written yet.
    /// </summary>
    public an_observer_with_a_handover_log() => _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _writes : null!;

    protected override ObserverState PersistedObserverState => _writes.PersistedState;

    void Establish()
    {
        var outstanding = new List<JobState>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => Delivered(call.Arg<IEnumerable<AppendedEvent>>()));
        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call => CreateCursor(Read(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSequenceNumber>(1), call.ArgAt<EventSourceId?>(2), call.ArgAt<IEnumerable<EventType>?>(3))));
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>())
            .Returns(call => Read(call.ArgAt<EventSequenceNumber>(0), EventSequenceNumber.Max, call.ArgAt<EventSourceId?>(2), call.ArgAt<IEnumerable<EventType>?>(1))
                .Select(_ => _.Context.SequenceNumber).FirstOrDefault(EventSequenceNumber.Unavailable));
        _eventSequence.GetTailSequenceNumber().Returns(_ => _log.Count == 0 ? EventSequenceNumber.Unavailable : _log[^1].Context.SequenceNumber);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns(call => Read(EventSequenceNumber.First, EventSequenceNumber.Max, null, call.Arg<IEnumerable<EventType>>())
                .Select(_ => _.Context.SequenceNumber).LastOrDefault(EventSequenceNumber.Unavailable));
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(call =>
            {
                var request = call.Arg<CatchUpObserverPartitionRequest>();
                _repairs.Add(request);
                var job = new JobState { Id = JobId.New(), Status = JobStatus.Running, Request = request };
                outstanding.Add(job);
                HasOutstandingJobs([.. outstanding]);
                return Result<JobId, StartJobError>.Success(job.Id);
            });
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(call =>
            {
                _wide = call.Arg<CatchUpObserverRequest>();
                return Result<JobId, StartJobError>.Success(JobId.New());
            });
        var index = Substitute.For<IObserverKeyIndex>();
        index.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(call => new ObserverKeysForTesting(
            Read(call.Arg<EventSequenceNumber>(), EventSequenceNumber.Max, null, [event_type]).Select(PartitionOf).Distinct().ToArray()));
        _eventStoreNamespaceStorage.ObserverKeyIndexes.GetFor(_observerKey, Arg.Any<ObserverReaderScope>()).Returns(index);
    }

    protected static Key PartitionOf(AppendedEvent @event) => (Key)@event.Context.EventSourceId.Value;

    protected AppendedEvent Append(string partition, ulong number, EventType? type = null)
    {
        var @event = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(type ?? event_type, number);
        @event = @event with { Context = @event.Context with { EventSourceId = partition } };
        _log.Add(@event);
        return @event;
    }

    protected ObserverSubscriberResult Delivered(IEnumerable<AppendedEvent> events)
    {
        var numbers = events.Select(_ => _.Context.SequenceNumber).ToArray();
        _delivered.AddRange(numbers);
        return ObserverSubscriberResult.Ok(numbers[^1]);
    }

    protected async Task<EventSequenceNumber> DeliverRange(EventSequenceNumber from, EventSequenceNumber to, IEnumerable<EventType> types, Key? partition = null)
    {
        using var cursor = await _eventSequenceStorage.GetRange(from, to, partition is null ? null : (EventSourceId)partition.ToString(), types);
        var last = EventSequenceNumber.Unavailable;
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                await _subscriber.OnNext(PartitionOf(@event), [@event], new ObserverSubscriberContext(null));
                last = @event.Context.SequenceNumber;
            }
        }
        return last;
    }

    protected async Task FinishRepair(CatchUpObserverPartitionRequest repair)
    {
        var last = await DeliverRange(repair.FromSequenceNumber, repair.ToSequenceNumber, repair.EventTypes, repair.Key);
        await _observer.HandoverRepairCaughtUp(repair.Key, repair.FromSequenceNumber, repair.ToSequenceNumber.Next(), last, succeeded: true, repair.OwnershipEpoch);
    }

    internal static IEventCursor CreateCursor(AppendedEvent[] events)
    {
        var cursor = Substitute.For<IEventCursor>();
        var first = true;
        cursor.Current.Returns(events);
        cursor.MoveNext().Returns(_ =>
        {
            var available = first && events.Length > 0;
            first = false;
            return available;
        });
        return cursor;
    }

    AppendedEvent[] Read(EventSequenceNumber from, EventSequenceNumber to, EventSourceId? partition, IEnumerable<EventType>? types) =>
        _log.Where(_ => _.Context.SequenceNumber >= from && _.Context.SequenceNumber <= to &&
            (partition is null || _.Context.EventSourceId == partition) && (types?.Contains(_.Context.EventType) != false)).ToArray();
}
