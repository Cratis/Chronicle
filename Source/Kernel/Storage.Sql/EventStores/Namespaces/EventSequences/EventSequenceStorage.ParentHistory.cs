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
        foreach (var chunk in sources.Chunk(512))
        {
            var query = scope.DbContext.Events.AsNoTracking().Where(e => chunk.Contains(e.EventSourceId) && types.Contains(e.Type) && e.SequenceNumber <= upTo.Value);
            var entries = await ParentHistoryWindow(query, from, upTo, chunk.Length).OrderBy(e => e.SequenceNumber).ToListAsync(cancellationToken);
            var events = await MaterializeParentHistory(entries, scope, cancellationToken);
            foreach (var group in events.GroupBy(e => e.Context.EventSourceId))
            {
                result[group.Key] = group.ToArray();
            }
        }
        return result;
    }

    static IQueryable<EventEntry> ParentHistoryWindow(IQueryable<EventEntry> query, EventSequenceNumber from, EventSequenceNumber upTo, int sourceCount)
    {
        // The common live/single-candidate lookup is one indexed TOP/LIMIT query.
        if (sourceCount == 1)
        {
            if (from > upTo)
            {
                return query.OrderByDescending(e => e.SequenceNumber).Take(1);
            }

            var predecessor = query.Where(e => e.SequenceNumber < from.Value).OrderByDescending(e => e.SequenceNumber).Take(1);
            return query.Where(e => e.SequenceNumber >= from.Value).Union(predecessor);
        }

        var predecessors = query.Where(e => e.SequenceNumber < from.Value).GroupBy(e => e.EventSourceId).Select(g => g.Max(e => e.SequenceNumber));
        return query.Where(e => e.SequenceNumber >= from.Value || predecessors.Contains(e.SequenceNumber));
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
