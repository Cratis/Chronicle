// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries;

/// <summary>
/// Represents the narrowing a user configured on a saved event sequence query, stored as a JSON column.
/// </summary>
public class SequenceQueryFilter
{
    /// <summary>
    /// Gets or sets the event source to narrow to, or empty for every event source.
    /// </summary>
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type identifiers to narrow to, or empty for every event type.
    /// </summary>
    public IList<string> EventTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the tags to narrow to, or empty for every event.
    /// </summary>
    public IList<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the inclusive lower bound on when the event occurred.
    /// </summary>
    public DateTimeOffset? OccurredFrom { get; set; }

    /// <summary>
    /// Gets or sets the exclusive upper bound on when the event occurred.
    /// </summary>
    public DateTimeOffset? OccurredTo { get; set; }
}
