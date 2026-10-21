// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts closed scope query and command values.
/// </summary>
public static class ClosedStreamConverters
{
    /// <summary>
    /// Convert optional wire dimensions to a normalized scope.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="eventStreamType">The stream type.</param>
    /// <param name="eventStreamId">The stream identifier.</param>
    /// <returns>The normalized scope.</returns>
    public static ClosedStreamScope ToScope(string? eventSourceId, string? eventSourceType, string? eventStreamType, string? eventStreamId) =>
        new ClosedStreamScope(
            eventSourceId is null ? null : new EventSourceId(eventSourceId),
            eventSourceType is null ? null : new EventSourceType(eventSourceType),
            eventStreamType is null ? null : new EventStreamType(eventStreamType),
            eventStreamId is null ? null : new EventStreamId(eventStreamId)).Normalized();

    /// <summary>
    /// Convert a stored closure to its inspection read model.
    /// </summary>
    /// <param name="closure">The persisted closure.</param>
    /// <returns>The inspection read model.</returns>
    public static ClosedStream ToReadModel(Concepts.Events.Constraints.ClosedStream closure) => new(
        closure.Scope.EventSourceId?.Value,
        closure.Scope.EventSourceType?.Value,
        closure.Scope.EventStreamType?.Value,
        closure.Scope.EventStreamId?.Value,
        closure.Owner == ClosedStreamOwner.Manual ? ClosedStreamOrigin.CompleteStream : ClosedStreamOrigin.ClosingEvent,
        closure.Owner == ClosedStreamOwner.Manual ? null : closure.Owner.Value,
        closure.SequenceNumber,
        closure.ClosedAt);
}
