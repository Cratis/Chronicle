// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines event-driven owned stream closures and exact reopening transitions.
/// </summary>
/// <param name="Name">The owning constraint name.</param>
/// <param name="MessageCallback">The violation message callback.</param>
/// <param name="EventTypeIds">The closing event types.</param>
/// <param name="Dimensions">The participating dimensions.</param>
/// <param name="ReopenedBy">The reopening event types.</param>
/// <param name="EventStreamIdFrom">The optional payload property providing the stream identifier.</param>
/// <remarks>
/// Registration is not retroactive. A reindex preserves this owner's current rows when a redacted
/// property-sourced closing or reopening event cannot be reconstructed from a closure snapshot.
/// The kernel emits a structured warning and continues rebuilding other constraints.
/// </remarks>
public record ClosesStreamConstraintDefinition(
    ConstraintName Name,
    ConstraintViolationMessageProvider MessageCallback,
    IEnumerable<EventTypeId> EventTypeIds,
    ClosedStreamDimensions Dimensions,
    IEnumerable<EventTypeId> ReopenedBy,
    string? EventStreamIdFrom = default) : IConstraintDefinition
{
    /// <summary>
    /// Gets the event sequences this declaration applies to; empty means all sequences.
    /// </summary>
    public IEnumerable<EventSequenceId> EventSequences { get; init; } = [];
}
