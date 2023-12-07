// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.EventSequences;

/// <summary>
/// Extension methods for querying events.
/// </summary>
public static class EventSequenceQueries
{
    /// <summary>
    /// Sort by sequence number.
    /// </summary>
    /// <param name="query">The query to sort.</param>
    /// <returns>The resulting query for continuation.</returns>
    public static IFindFluent<Event, Event> SortBySequenceNumber(this IFindFluent<Event, Event> query) => query.SortBy(_ => _.SequenceNumber.Value);

    /// <summary>
    /// Sort by sequence number descending.
    /// </summary>
    /// <param name="query">The query to sort.</param>
    /// <returns>The resulting query for continuation.</returns>
    public static IFindFluent<Event, Event> SortByDescendingSequenceNumber(this IFindFluent<Event, Event> query) => query.SortByDescending(_ => _.SequenceNumber.Value);
}
