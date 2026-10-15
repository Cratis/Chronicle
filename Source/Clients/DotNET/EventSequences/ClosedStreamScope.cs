// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Identifies a nonempty subset of event dimensions to complete or inspect.
/// </summary>
/// <param name="EventSourceId">The event source identifier, or null.</param>
/// <param name="EventSourceType">The event source type, or null.</param>
/// <param name="EventStreamType">The stream type, or null.</param>
/// <param name="EventStreamId">The stream identifier, or null.</param>
/// <remarks>
/// Unspecified event source values and empty stream values are treated as unset. All and Default stream values remain real
/// dimensions, allowing a particular event source's default stream to be closed.
/// </remarks>
public record ClosedStreamScope(
    EventSourceId? EventSourceId = default,
    EventSourceType? EventSourceType = default,
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default)
{
    /// <summary>
    /// Gets whether the scope has no participating dimensions.
    /// </summary>
    public bool IsEmpty => (EventSourceId is null || EventSourceId == Events.EventSourceId.Unspecified) &&
        (EventSourceType is null || EventSourceType == Events.EventSourceType.Unspecified) &&
        string.IsNullOrEmpty(EventStreamType?.Value) && string.IsNullOrEmpty(EventStreamId?.Value);

    /// <summary>
    /// Create a stream-type and stream-identifier scope.
    /// </summary>
    /// <param name="eventStreamType">The stream type.</param>
    /// <param name="eventStreamId">The stream identifier.</param>
    /// <returns>The scope.</returns>
    public static ClosedStreamScope ForStream(EventStreamType eventStreamType, EventStreamId eventStreamId) =>
        new(EventStreamType: eventStreamType, EventStreamId: eventStreamId);

    /// <summary>
    /// Create a scope covering all streams of one event source.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <returns>The scope.</returns>
    public static ClosedStreamScope ForEventSource(EventSourceId eventSourceId) => new(EventSourceId: eventSourceId);
}
