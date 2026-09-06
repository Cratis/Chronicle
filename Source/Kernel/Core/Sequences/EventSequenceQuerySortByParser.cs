// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Translates the sorting a caller asked for into what an event sequence query is ordered by.
/// </summary>
public static class EventSequenceQuerySortByParser
{
    /// <summary>
    /// Work out what to order an event sequence query by, from the sorting Arc resolved for it.
    /// </summary>
    /// <param name="sorting">The <see cref="Sorting"/> from the query context.</param>
    /// <returns>The field to order by, and whether the order runs downwards.</returns>
    /// <remarks>
    /// Arc carries sorting on the query context rather than as an argument, so this reads what the
    /// caller asked for rather than inventing a parameter of its own. A field the sequence cannot be
    /// ordered on falls back to its natural order rather than failing the request - the caller is
    /// asking for a view of the data, not making an assertion about it.
    /// </remarks>
    public static (EventSequenceQuerySortBy SortBy, bool Descending) From(Sorting sorting) =>
        (Enum.TryParse<EventSequenceQuerySortBy>(sorting.Field, ignoreCase: true, out var parsed)
            ? parsed
            : EventSequenceQuerySortBy.SequenceNumber,
        sorting.Direction == SortDirection.Descending);
}
