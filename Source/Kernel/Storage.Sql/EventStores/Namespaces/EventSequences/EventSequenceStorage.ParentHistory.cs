// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>>> GetInstancesOfAnyInRange(
        IEnumerable<EventSourceId> eventSourceIds,
        IEnumerable<EventTypeId> eventTypes,
        EventSequenceNumber from,
        EventSequenceNumber upTo,
        CancellationToken cancellationToken = default)
    {
        var sources = eventSourceIds.Distinct().ToArray();
        var types = eventTypes.Distinct().ToArray();
        var result = new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>>();
        cancellationToken.ThrowIfCancellationRequested();
        if (sources.Length == 0 || types.Length == 0)
        {
            return result;
        }

        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        foreach (var source in sources)
        {
            var query = scope.DbContext.Events.AsNoTracking().Where(e => e.EventSourceId == source && e.SequenceNumber <= upTo.Value);
            var entries = await ParentHistoryWindow(query, types, from, upTo, cancellationToken);
            if (entries.Count > 0)
            {
                result[source] = await MaterializeParentHistory(entries, scope, cancellationToken);
            }
        }
        return result;
    }

    static async Task<List<EventEntry>> ParentHistoryWindow(IQueryable<EventEntry> query, EventTypeId[] types, EventSequenceNumber from, EventSequenceNumber upTo, CancellationToken cancellationToken)
    {
        var predecessor = await ParentHistoryPredecessor(query, types, from, cancellationToken);
        List<EventEntry> entries = predecessor is null ? [] : [predecessor];
        if (from <= upTo)
        {
            var window = await query.Where(e => types.Contains(e.Type) && e.SequenceNumber >= from.Value)
                .OrderBy(e => e.SequenceNumber).ToListAsync(cancellationToken);
            entries.AddRange(window);
        }
        return entries;
    }

    static async Task<EventEntry?> ParentHistoryPredecessor(IQueryable<EventEntry> query, EventTypeId[] types, EventSequenceNumber from, CancellationToken cancellationToken)
    {
        EventEntry? predecessor = null;
        foreach (var type in types)
        {
            var candidate = await query.Where(e => e.Type == type && e.SequenceNumber < from.Value)
                .OrderByDescending(e => e.SequenceNumber).FirstOrDefaultAsync(cancellationToken);
            if (candidate is not null && (predecessor is null || candidate.SequenceNumber > predecessor.SequenceNumber))
            {
                predecessor = candidate;
            }
        }
        return predecessor;
    }

    async Task<List<AppendedEvent>> MaterializeParentHistory(List<EventEntry> entries, DbContextScope<EventSequenceDbContext> scope, CancellationToken cancellationToken)
    {
        var namedTags = await NamedTagEntries.LoadFor(scope.DbContext, eventSequenceId.Value, entries.Select(e => e.SequenceNumber), cancellationToken);
        var schemas = new EventSchemaResolver(eventTypesStorage);
        var events = new List<AppendedEvent>(entries.Count);
        foreach (var entry in entries)
        {
            events.Add(await EventEntryConverter.ToAppendedEvent(entry, eventStore, @namespace, identityStorage, NamedTagEntries.At(namedTags, entry.SequenceNumber), schemas.GetFor, converter));
        }
        return events;
    }
}
