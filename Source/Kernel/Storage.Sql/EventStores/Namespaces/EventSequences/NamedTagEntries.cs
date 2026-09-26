// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>Loads named tags for a bounded batch of events without one query per event.</summary>
public static class NamedTagEntries
{
    /// <summary>Loads all named tags for the given sequence numbers.</summary>
    /// <param name="context">The event sequence context.</param>
    /// <param name="sequenceId">The sequence table name.</param>
    /// <param name="numbers">The sequence numbers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Tags grouped by event sequence number.</returns>
    public static async Task<IReadOnlyDictionary<ulong, IReadOnlyCollection<NamedTag>>> LoadFor(
        EventSequenceDbContext context, string sequenceId, IEnumerable<ulong> numbers, CancellationToken cancellationToken = default)
    {
        var requested = numbers.ToArray();
        if (requested.Length == 0)
        {
            return new Dictionary<ulong, IReadOnlyCollection<NamedTag>>();
        }

        var rows = await context.NamedTags.AsNoTracking()
            .Where(tag => tag.EventSequenceId == sequenceId && requested.Contains(tag.SequenceNumber))
            .OrderBy(tag => tag.SequenceNumber).ThenBy(tag => tag.Position)
            .ToListAsync(cancellationToken);

        return rows.GroupBy(tag => tag.SequenceNumber)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<NamedTag>)group.Select(tag => tag.ToNamedTag()).ToArray());
    }

    /// <summary>Gets tags for one event, or an empty collection for legacy events.</summary>
    /// <param name="tags">Tags grouped by sequence number.</param>
    /// <param name="number">The event sequence number.</param>
    /// <returns>The named tags.</returns>
    public static IReadOnlyCollection<NamedTag> At(IReadOnlyDictionary<ulong, IReadOnlyCollection<NamedTag>> tags, ulong number) =>
        tags.TryGetValue(number, out var result) ? result : [];
}
