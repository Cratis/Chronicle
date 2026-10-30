// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    bool _appendTailUncertain;

    static bool MatchesAppend(AppendedEvent stored, EventToAppendToStorage attempted) =>
        stored.Context.SequenceNumber == attempted.SequenceNumber &&
        stored.Context.CorrelationId == attempted.CorrelationId &&
        stored.Context.EventType.Id == attempted.EventType.Id &&
        stored.Context.EventSourceId == attempted.EventSourceId &&
        stored.GenerationalHashes.TryGetValue((int)attempted.EventType.Generation.Value, out var hash) &&
        hash is not null && hash != EventHash.NotSet &&
        attempted.ContentHashes.TryGetValue(attempted.EventType.Generation, out var submitted) &&
        hash == submitted;

    async Task RefreshUncertainAppendTail()
    {
        if (!_appendTailUncertain) return;
        var tail = await EventSequenceStorage.GetTailSequenceNumber();
        if (tail != EventSequenceNumber.Unavailable && tail >= State.SequenceNumber)
        {
            var first = State.SequenceNumber;
            var adoptedTails = new Dictionary<EventTypeId, EventSequenceNumber>();
            using var cursor = await EventSequenceStorage.GetRange(first, tail);
            while (await cursor.MoveNext())
            {
                foreach (var @event in cursor.Current)
                {
                    var context = @event.Context;
                    if (!adoptedTails.TryGetValue(context.EventType.Id, out var previous) || previous < context.SequenceNumber)
                        adoptedTails[context.EventType.Id] = context.SequenceNumber;
                }
            }
            MarkConstraintIndexGap(first);
            foreach (var (type, number) in adoptedTails)
            {
                if (!State.TailSequenceNumberPerEventType.TryGetValue(type, out var previous) || previous < number)
                    State.TailSequenceNumberPerEventType[type] = number;
            }
            State.SequenceNumber = tail.Next();
            await SpillAppendedEventsQueuesToCatchup();
        }
        _appendTailUncertain = false;
    }

    async Task MarkAppendOutcomeUnknown(EventSequenceNumber first)
    {
        _appendTailUncertain = true;
        MarkConstraintIndexGap(first);
        await SpillAppendedEventsQueuesToCatchup();
        _metrics?.AppendOutcomeUnknown();
        logger.AppendOutcomeUnknown(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, first);
    }

    async Task<List<AppendedEvent>?> ReadBackAppend(List<EventToAppendToStorage> attempted)
    {
        if (attempted.Count == 0) return [];
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            using var cursor = await EventSequenceStorage.ReadBackAppend(attempted[0].SequenceNumber, attempted[^1].SequenceNumber, deadline.Token).WaitAsync(deadline.Token);
            var stored = new List<AppendedEvent>();
            while (await cursor.MoveNext().WaitAsync(deadline.Token))
            {
                stored.AddRange(cursor.Current);
                if (stored.Count > attempted.Count) return null;
            }
            if (stored.Count != attempted.Count) return null;
            stored.Sort((left, right) => left.Context.SequenceNumber.CompareTo(right.Context.SequenceNumber));
            return stored.Zip(attempted).All(pair => MatchesAppend(pair.First, pair.Second)) ? stored : null;
        }
        catch (Exception)
        {
            // A failed or timed-out probe cannot establish absence, even when it read no events.
            return null;
        }
    }

    async Task CompleteConfirmedAppend(
        List<AppendedEvent> events,
        IEnumerable<(Events.Constraints.ConstraintValidationContext Context, EventSequenceNumber SequenceNumber)> updates)
    {
        try
        {
            foreach (var @event in events)
            {
                var context = @event.Context;
                if (!State.TailSequenceNumberPerEventType.TryGetValue(context.EventType.Id, out var tail) || tail < context.SequenceNumber)
                    State.TailSequenceNumberPerEventType[context.EventType.Id] = context.SequenceNumber;
            }
            await CompleteDurableAppend(events, updates);
            logger.AppendManyReceived(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, events.Count);
            foreach (var @event in events) _metrics?.AppendedEvent(@event.Context.EventType.Id);
        }
        catch (Exception error)
        {
            // The commit is known. A post-commit fault must not escape to an append failure handler.
            MarkConstraintIndexGap(events[0].Context.SequenceNumber);
            logger.ErrorAppendingMany(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, error);
            await SpillAppendedEventsQueuesToCatchup();
        }
    }
}
