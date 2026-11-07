// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Reads a storage-limited window once, preserving the query's sequence-number ordering without offsets.
/// </summary>
/// <param name="query">The filtered, ordered and limited query.</param>
/// <param name="scope">The context scope owned by this cursor.</param>
/// <param name="convert">The generation-aware event converter.</param>
/// <param name="cancellationToken">The cancellation token.</param>
public class EventWindowCursor(
    IQueryable<EventEntry> query,
    DbContextScope<EventSequenceDbContext> scope,
    Func<EventEntry, IReadOnlyCollection<NamedTag>, Task<AppendedEvent>> convert,
    CancellationToken cancellationToken) : IEventCursor
{
    bool _read;

    /// <inheritdoc/>
    public IEnumerable<AppendedEvent> Current { get; private set; } = [];

    /// <inheritdoc/>
    public async Task<bool> MoveNext()
    {
        if (_read)
        {
            Current = [];
            return false;
        }

        _read = true;
        var entries = await query.ToListAsync(cancellationToken);
        var namedTags = await NamedTagEntries.LoadFor(scope.DbContext, scope.DbContext.TableName, entries.Select(_ => _.SequenceNumber), cancellationToken);
        var events = new List<AppendedEvent>(entries.Count);
        foreach (var entry in entries)
        {
            events.Add(await convert(entry, NamedTagEntries.At(namedTags, entry.SequenceNumber)));
        }

        Current = events;
        return entries.Count > 0;
    }

    /// <inheritdoc/>
    public void Dispose() => scope.Dispose();
}
