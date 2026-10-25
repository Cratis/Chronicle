// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage
{
    /// <summary>
    /// Indexes stable list positions: updates replace events in place and appends only add to the list.
    /// </summary>
    readonly SortedList<ulong, int> _sequencePositions = [];

    /// <inheritdoc/>
    public Task<IEventCursor> GetWindow(
        EventSequenceNumber from,
        EventSequenceNumber until,
        int limit,
        bool descending,
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default,
        IEnumerable<Tag>? tags = default,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1)
        {
            throw new InvalidEventSequenceWindowLimit(limit);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var criteria = new EventSequenceQueryCriteria(
            eventSourceId,
            eventSourceType?.IsDefaultOrUnspecified == false ? eventSourceType : null,
            eventStreamType,
            EventTypes: eventTypes?.ToArray(),
            Tags: tags?.ToArray());
        lock (_lock)
        {
            var start = descending ? WindowPosition(until.Value, true) - 1 : WindowPosition(from.Value, false);
            var positions = WindowPositions(start, from.Value, until.Value, descending);
            var matching = positions.Select(position => _events[_sequencePositions.Values[position]])
                .Where(@event => criteria.Matches(@event.Context) && MatchesStream(@event, eventStreamId));
            var page = new List<AppendedEvent>();
            foreach (var @event in matching.Take(limit))
            {
                cancellationToken.ThrowIfCancellationRequested();
                page.Add(WithGenerationMetadata(@event));
            }

            return Task.FromResult<IEventCursor>(new EventCursor(page));
        }
    }

    static bool MatchesStream(AppendedEvent @event, EventStreamId? eventStreamId) =>
        eventStreamId is not { Value.Length: > 0, IsDefault: false } || @event.Context.EventStreamId == eventStreamId;

    void AddEvent(AppendedEvent @event)
    {
        _sequencePositions.Add(@event.Context.SequenceNumber.Value, _events.Count);
        _events.Add(@event);
    }

    AppendedEvent WithGenerationMetadata(AppendedEvent @event) => @event with
    {
        Context = @event.Context with
        {
            AppendedGeneration = _appendedGenerations.TryGetValue(@event.Context.SequenceNumber, out var generation) ? new EventTypeGeneration(generation) : null
        },
        GenerationalHashes = _generationHashes.TryGetValue(@event.Context.SequenceNumber, out var hashes)
            ? hashes.ToImmutableDictionary(_ => (int)_.Key.Value, _ => _.Value)
            : new Dictionary<int, EventHash>()
    };

    int WindowPosition(ulong bound, bool after)
    {
        var lower = 0;
        var upper = _sequencePositions.Count;
        while (lower < upper)
        {
            var middle = lower + ((upper - lower) / 2);
            var number = _sequencePositions.Keys[middle];
            if (after ? number <= bound : number < bound)
            {
                lower = middle + 1;
            }
            else
            {
                upper = middle;
            }
        }

        return lower;
    }

    IEnumerable<int> WindowPositions(int start, ulong from, ulong until, bool descending)
    {
        for (var position = start; position >= 0 && position < _sequencePositions.Count; position += descending ? -1 : 1)
        {
            var number = _sequencePositions.Keys[position];
            if (number < from || number > until)
            {
                yield break;
            }

            yield return position;
        }
    }
}
