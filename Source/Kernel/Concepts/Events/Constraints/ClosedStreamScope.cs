// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Identifies a subset of event dimensions. Unset dimensions do not constrain the scope.
/// Empty stream values and unspecified event source values normalize to unset dimensions.
/// </summary>
/// <param name="EventSourceId">The event source identifier, or null.</param>
/// <param name="EventSourceType">The event source type, or null.</param>
/// <param name="EventStreamType">The stream type, or null.</param>
/// <param name="EventStreamId">The stream identifier, or null.</param>
public record ClosedStreamScope(
    EventSourceId? EventSourceId = default,
    EventSourceType? EventSourceType = default,
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default)
{
    /// <summary>
    /// Gets the participating dimensions after normalization.
    /// </summary>
    public ClosedStreamDimensions Dimensions =>
        (EventSourceId is not null && EventSourceId != Events.EventSourceId.Unspecified ? ClosedStreamDimensions.EventSourceId : ClosedStreamDimensions.None) |
        (EventSourceType is not null && EventSourceType != Events.EventSourceType.Unspecified ? ClosedStreamDimensions.EventSourceType : ClosedStreamDimensions.None) |
        (EventStreamType?.Value.Length > 0 ? ClosedStreamDimensions.EventStreamType : ClosedStreamDimensions.None) |
        (EventStreamId?.Value.Length > 0 ? ClosedStreamDimensions.EventStreamId : ClosedStreamDimensions.None);

    /// <summary>
    /// Gets whether the scope has no participating dimensions.
    /// </summary>
    public bool IsEmpty => Dimensions == ClosedStreamDimensions.None;

    /// <summary>
    /// Gets whether the scope covers only default stream values without a specific event source.
    /// </summary>
    public bool IsDefaultStreamOnly => !IsEmpty &&
        (EventSourceId is null || EventSourceId == Events.EventSourceId.Unspecified) &&
        EventSourceType?.IsDefaultOrUnspecified != false &&
        (string.IsNullOrEmpty(EventStreamType?.Value) || EventStreamType == Events.EventStreamType.All) &&
        (string.IsNullOrEmpty(EventStreamId?.Value) || EventStreamId?.Value == Events.EventStreamId.Default);

    /// <summary>
    /// Normalize unspecified event source dimensions and empty stream values to unset dimensions.
    /// </summary>
    /// <returns>The normalized scope.</returns>
    public ClosedStreamScope Normalized() => this with
    {
        EventSourceId = EventSourceId == Events.EventSourceId.Unspecified ? null : EventSourceId,
        EventSourceType = EventSourceType == Events.EventSourceType.Unspecified ? null : EventSourceType,
        EventStreamType = EventStreamType?.Value.Length == 0 ? null : EventStreamType,
        EventStreamId = EventStreamId?.Value.Length == 0 ? null : EventStreamId
    };

    /// <summary>
    /// Check whether this scope covers another scope.
    /// </summary>
    /// <param name="other">The scope to check.</param>
    /// <returns>True if all participating dimensions match and this scope is nonempty.</returns>
    public bool Covers(ClosedStreamScope other)
    {
        var closure = Normalized();
        var target = other.Normalized();

        return !closure.IsEmpty && (closure.Dimensions & target.Dimensions) == closure.Dimensions &&
            (closure.EventSourceId is null || closure.EventSourceId == target.EventSourceId) &&
            (closure.EventSourceType is null || closure.EventSourceType == target.EventSourceType) &&
            (closure.EventStreamType is null || closure.EventStreamType == target.EventStreamType) &&
            (closure.EventStreamId is null || closure.EventStreamId == target.EventStreamId);
    }

    /// <summary>
    /// Select the dimensions of an append participating in a scope.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="eventStreamType">The stream type.</param>
    /// <param name="eventStreamId">The stream identifier.</param>
    /// <param name="mask">The dimensions to select.</param>
    /// <returns>The normalized scope.</returns>
    public static ClosedStreamScope ForAppend(EventSourceId eventSourceId, EventSourceType eventSourceType, EventStreamType eventStreamType, EventStreamId eventStreamId, ClosedStreamDimensions mask) =>
        new ClosedStreamScope(
            mask.HasFlag(ClosedStreamDimensions.EventSourceId) ? eventSourceId : null,
            mask.HasFlag(ClosedStreamDimensions.EventSourceType) ? eventSourceType : null,
            mask.HasFlag(ClosedStreamDimensions.EventStreamType) ? eventStreamType : null,
            mask.HasFlag(ClosedStreamDimensions.EventStreamId) ? eventStreamId : null).Normalized();
}
