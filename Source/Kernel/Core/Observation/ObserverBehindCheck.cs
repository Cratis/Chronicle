// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverBehindCheck"/>.
/// </summary>
/// <remarks>
/// The cursors are read before the tails. An event appended in between is then part of the tail but not of the cursor, so
/// the observer is found behind once more than it needed to be; the other order could miss it. A redaction stands in for
/// the event type it redacted, so it is part of what every observer of types looks at.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> the cursors are read from.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> the event sequences are reached through for their tails.</param>
public class ObserverBehindCheck(IStorage storage, IGrainFactory grainFactory) : IObserverBehindCheck
{
    /// <inheritdoc/>
    public async Task<ObserverBehindCheckResult> Check(EventSequenceKey eventSequence, IReadOnlyCollection<ObserverBehindCheckCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return new([], [], 0);
        }

        var cursors = await storage
            .GetEventStore(eventSequence.EventStore)
            .GetNamespace(eventSequence.Namespace)
            .Observers
            .GetNextEventSequenceNumbers(eventSequence.EventSequenceId);
        var comparable = candidates
            .Where(candidate => cursors.TryGetValue(candidate.ObserverKey.ObserverId, out var cursor) && cursor.IsActualValue)
            .ToArray();
        var tails = await ReadTails(grainFactory.GetGrain<IEventSequence>(eventSequence), comparable);

        var behind = new List<ObserverKey>();
        var unverified = new List<ObserverKey>();
        var upToDate = 0;
        foreach (var candidate in candidates)
        {
            if (!cursors.TryGetValue(candidate.ObserverKey.ObserverId, out var cursor) || !cursor.IsActualValue)
            {
                unverified.Add(candidate.ObserverKey);
            }
            else if (tails.HasEventsFrom(candidate, cursor))
            {
                behind.Add(candidate.ObserverKey);
            }
            else
            {
                upToDate++;
            }
        }

        return new(behind, unverified, upToDate);
    }

    static async Task<Tails> ReadTails(IEventSequence eventSequence, ObserverBehindCheckCandidate[] candidates)
    {
        if (candidates.Length == 0)
        {
            return new(new Dictionary<EventTypeId, EventSequenceNumber>(), EventSequenceNumber.Unavailable);
        }

        var typeIds = candidates
            .Where(candidate => !candidate.AllEventTypes)
            .SelectMany(candidate => candidate.EventTypes.Ids)
            .Append(GlobalEventTypes.Redaction)
            .Distinct()
            .ToArray();

        var perType = candidates.All(candidate => candidate.AllEventTypes)
            ? new Dictionary<EventTypeId, EventSequenceNumber>()
            : await ReadTailPerEventType(eventSequence, typeIds);
        var tail = candidates.Any(candidate => candidate.AllEventTypes)
            ? await eventSequence.GetTailSequenceNumber()
            : EventSequenceNumber.Unavailable;
        return new(perType, tail);
    }

    static async Task<Dictionary<EventTypeId, EventSequenceNumber>> ReadTailPerEventType(IEventSequence eventSequence, EventTypeId[] typeIds)
    {
        var tails = await Task.WhenAll(typeIds.Select(typeId => eventSequence.GetTailSequenceNumberForEventTypes([new EventType(typeId, EventTypeGeneration.First)])));
        return typeIds.Zip(tails).ToDictionary(pair => pair.First, pair => pair.Second);
    }

    /// <summary>
    /// The tails of an event sequence that the cursors are compared with.
    /// </summary>
    /// <param name="PerEventType">The sequence number of the last event of each event type that was looked up.</param>
    /// <param name="Tail">The sequence number of the last event of the sequence, or unavailable when it was not looked up.</param>
    sealed record Tails(IReadOnlyDictionary<EventTypeId, EventSequenceNumber> PerEventType, EventSequenceNumber Tail)
    {
        public bool HasEventsFrom(ObserverBehindCheckCandidate candidate, EventSequenceNumber cursor)
        {
            if (candidate.AllEventTypes)
            {
                return Tail.IsActualValue && Tail >= cursor;
            }

            return candidate.EventTypes.Ids.Append(GlobalEventTypes.Redaction).Any(typeId =>
                PerEventType.TryGetValue(typeId, out var tail) && tail.IsActualValue && tail >= cursor);
        }
    }
}
