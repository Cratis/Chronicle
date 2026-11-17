// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<EventCount> GetCount(
        EventSequenceNumber? lastEventSequenceNumber = null,
        IEnumerable<EventType>? eventTypes = null,
        IEnumerable<Tag>? tags = null)
    {
        var snapshot = Events;
        var filtered = Filter(snapshot, new() { EventTypes = eventTypes, Tags = tags });
        if (lastEventSequenceNumber is not null)
        {
            filtered = filtered.Where(_ => _.Context.SequenceNumber <= lastEventSequenceNumber);
        }

        return Task.FromResult((EventCount)filtered.Count());
    }

    /// <inheritdoc/>
    public Task<EventCount> GetCountMatching(EventSequenceQueryCriteria criteria) =>
        Task.FromResult((EventCount)Matching(Events, criteria).Count());

    /// <inheritdoc/>
    public Task<IEventCursor> GetPage(
        EventSequenceQueryCriteria criteria,
        int skip,
        int take,
        EventSequenceQuerySort? sort = null,
        CancellationToken cancellationToken = default)
    {
        var ordered = Sorted(Matching(Events, criteria), sort ?? EventSequenceQuerySort.Default);
        return Task.FromResult<IEventCursor>(new EventCursor([.. ordered.Skip(skip).Take(take)]));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<HistogramBucket>> GetHistogram(HistogramResolution resolution, EventSequenceQueryCriteria criteria)
    {
        var buckets = Matching(Events, criteria)
            .GroupBy(_ => HistogramResolutions.Truncate(_.Context.Occurred, resolution))
            .OrderBy(_ => _.Key)
            .Select(_ => new HistogramBucket(_.Key, _.LongCount()));

        return Task.FromResult<IEnumerable<HistogramBucket>>([.. buckets]);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetHeadSequenceNumber(
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        var filtered = Filter(Events, new() { EventSourceId = eventSourceId, EventTypes = eventTypes }).ToList();
        return filtered.Count == 0
            ? Task.FromResult(EventSequenceNumber.Unavailable)
            : Task.FromResult(filtered.Min(_ => _.Context.SequenceNumber)!);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null,
        EventSourceType? eventSourceType = null,
        EventStreamId? eventStreamId = null,
        EventStreamType? eventStreamType = null)
    {
        var filtered = Filter(Events, new() { EventSourceId = eventSourceId, EventSourceType = eventSourceType, EventStreamType = eventStreamType, EventStreamId = eventStreamId, EventTypes = eventTypes }).ToList();
        return filtered.Count == 0
            ? Task.FromResult(EventSequenceNumber.Unavailable)
            : Task.FromResult(filtered.Max(_ => _.Context.SequenceNumber)!);
    }

    /// <inheritdoc/>
    public Task<TailEventSequenceNumbers> GetTailSequenceNumbers(IEnumerable<EventType> eventTypes)
    {
        var snapshot = Events;
        var eventTypeList = eventTypes.ToImmutableList();
        var tail = snapshot.Count == 0 ? EventSequenceNumber.Unavailable : snapshot.Max(_ => _.Context.SequenceNumber)!;
        var filtered = Filter(snapshot, new() { EventTypes = eventTypeList }).ToList();
        var tailForEventTypes = filtered.Count == 0 ? EventSequenceNumber.Unavailable : filtered.Max(_ => _.Context.SequenceNumber)!;
        return Task.FromResult(new TailEventSequenceNumbers(eventSequenceId, eventTypeList, tail, tailForEventTypes));
    }

    /// <inheritdoc/>
    public Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(
        IEnumerable<EventType> eventTypes)
    {
        var snapshot = Events;
        var result = eventTypes.ToImmutableDictionary(
            et => et,
            et =>
            {
                var matching = snapshot.Where(_ => _.Context.EventType.Id == et.Id).ToList();
                return matching.Count == 0
                    ? EventSequenceNumber.Unavailable
                    : matching.Max(_ => _.Context.SequenceNumber)!;
            });

        return Task.FromResult<IImmutableDictionary<EventType, EventSequenceNumber>>(result);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        var filtered = Filter(Events, new() { EventSourceId = eventSourceId, EventTypes = eventTypes })
            .Where(_ => _.Context.SequenceNumber >= sequenceNumber)
            .MinBy(_ => _.Context.SequenceNumber);

        return Task.FromResult(filtered?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) =>
        Task.FromResult(Events.Any(_ => _.Context.EventSourceId == eventSourceId));

    /// <inheritdoc/>
    public Task<Catch<Option<AppendedEvent>>> TryGetLastEventBefore(
        EventTypeId eventTypeId,
        EventSourceId eventSourceId,
        EventSequenceNumber currentSequenceNumber)
    {
        var found = Events
            .Where(_ =>
                _.Context.EventType.Id == eventTypeId &&
                _.Context.EventSourceId == eventSourceId &&
                _.Context.SequenceNumber < currentSequenceNumber)
            .MaxBy(_ => _.Context.SequenceNumber);

        return Task.FromResult<Catch<Option<AppendedEvent>>>(
            found is not null
                ? (Option<AppendedEvent>)found
                : Option<AppendedEvent>.None());
    }

    /// <inheritdoc/>
    public Task<AppendedEvent> GetEventAt(EventSequenceNumber sequenceNumber) =>
        Task.FromResult(Events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber)
            ?? throw new InvalidOperationException($"No event at sequence number {sequenceNumber}"));

    /// <inheritdoc/>
    public Task<Option<AppendedEvent>> TryGetLastInstanceOfAny(
        EventSourceId eventSourceId,
        IEnumerable<EventTypeId> eventTypes)
    {
        var eventTypeSet = eventTypes.ToHashSet();
        var found = Events
            .Where(_ =>
                _.Context.EventSourceId == eventSourceId &&
                eventTypeSet.Contains(_.Context.EventType.Id))
            .MaxBy(_ => _.Context.SequenceNumber);

        return Task.FromResult(found is not null
            ? (Option<AppendedEvent>)found
            : Option<AppendedEvent>.None());
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default,
        IEnumerable<Tag>? tags = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = Filter(Events, new() { EventSourceId = eventSourceId, EventSourceType = eventSourceType, EventStreamType = eventStreamType, EventStreamId = eventStreamId, EventTypes = eventTypes, Tags = tags })
            .Where(_ => _.Context.SequenceNumber >= sequenceNumber)
            .OrderBy(_ => _.Context.SequenceNumber)
            .ToList();

        return Task.FromResult<IEventCursor>(new EventCursor(filtered));
    }

    /// <inheritdoc/>
    public Task<IEventCursor> ReadBackAppend(EventSequenceNumber first, EventSequenceNumber last, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IEventCursor>(new EventCursor(_events.Where(e => e.Context.SequenceNumber >= first && e.Context.SequenceNumber <= last).ToList()));
        }
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default, IEnumerable<Tag>? tags = default, CancellationToken cancellationToken = default) =>
        GetRange(start, end, eventSourceId, eventTypes, tags, null, null, cancellationToken);

    /// <inheritdoc/>
    public Task<IEventCursor> GetRangeWithHistory(EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default, IEnumerable<Tag>? tags = default, CancellationToken cancellationToken = default) =>
        GetRange(start, end, eventSourceId, eventTypes, tags, cancellationToken);

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumberWithHistory(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, EventSourceType? eventSourceType = default, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, IEnumerable<EventType>? eventTypes = default, IEnumerable<Tag>? tags = default, CancellationToken cancellationToken = default) =>
        GetFromSequenceNumber(sequenceNumber, eventSourceId, eventSourceType, eventStreamType, eventStreamId, eventTypes, tags, cancellationToken);

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(
        EventSequenceNumber start,
        EventSequenceNumber end,
        EventSourceId? eventSourceId,
        IEnumerable<EventType>? eventTypes,
        IEnumerable<Tag>? tags,
        EventSourceType? eventSourceType,
        EventStreamType? eventStreamType,
        CancellationToken cancellationToken)
    {
        var filtered = Filter(Events, new() { EventSourceId = eventSourceId, EventSourceType = eventSourceType, EventStreamType = eventStreamType, EventTypes = eventTypes, Tags = tags })
            .Where(_ => _.Context.SequenceNumber >= start && _.Context.SequenceNumber <= end)
            .OrderBy(_ => _.Context.SequenceNumber)
            .ToList();

        return Task.FromResult<IEventCursor>(new EventCursor(filtered));
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetEventsWithLimit(
        EventSequenceNumber start,
        int limit,
        EventSourceId? eventSourceId = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default,
        IEnumerable<Tag>? tags = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = Filter(Events, new() { EventSourceId = eventSourceId, EventStreamType = eventStreamType, EventStreamId = eventStreamId, EventTypes = eventTypes, Tags = tags })
            .Where(_ => _.Context.SequenceNumber >= start)
            .OrderBy(_ => _.Context.SequenceNumber)
            .Take(limit)
            .ToList();

        return Task.FromResult<IEventCursor>(new EventCursor(filtered));
    }
}
