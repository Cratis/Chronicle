// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Declares an event that closes a stream scope after its durable append.
/// </summary>
/// <remarks>
/// Registering a declaration is not retroactive. Closing happens in the same kernel grain turn as the
/// append, but is not crash-atomic with it; an explicit reindex can repair a missing index update.
/// Reopening removes only this constraint's exact closure, never an operator's manual closure.
/// A reindex leaves this owner's rows untouched if a redacted property-sourced closing or reopening
/// event has no recoverable scope in the existing closure snapshot. It logs the constraint and event
/// position, then continues rebuilding other constraints; no reopening tombstones are retained.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ClosesStreamAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the participating dimensions; defaults to event source, stream type and stream identifier.
    /// </summary>
    public ClosedStreamDimensions Dimensions { get; set; } = ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId;

    /// <summary>
    /// Gets or sets the payload property providing the stream identifier, implying that dimension.
    /// </summary>
    public string? EventStreamIdFrom { get; set; }

    /// <summary>
    /// Gets or sets the event types that reopen this constraint's exact scope.
    /// </summary>
    public Type[] ReopenedBy { get; set; } = [];

    /// <summary>
    /// Gets or sets the owning constraint name; defaults to the declaring event type's name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the event sequences this declaration applies to; empty means all sequences.
    /// </summary>
    public string[] EventSequences { get; set; } = [];
}
