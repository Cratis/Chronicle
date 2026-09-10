// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.EventSequences.Concurrency;

/// <summary>
/// Represents the scope of concurrency for an event sequence operation.
/// </summary>
[ProtoContract]
public class ConcurrencyScope
{
    /// <summary>
    /// Gets or sets the expected sequence number for the event sequence operation.
    /// </summary>
    /// <remarks>
    /// A scope that expects no event matching its narrowing to exist carries the "unavailable" sequence number
    /// here and says what it means in <see cref="ExpectsNoMatchingEvent"/>. Putting a distinguished number in this
    /// field instead would read to a kernel that predates the field as an ordinary expected sequence number near
    /// the top of the range, which every real tail compares below - a check that reports success without ever
    /// having run. "Unavailable" is the value such a kernel already declines to validate, so it skips the check
    /// and says so, exactly as it does today.
    /// </remarks>
    [ProtoMember(1)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the value indicating whether to scope to the event source id.
    /// </summary>
    [ProtoMember(2)]
    public bool EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the optional event stream type to scope to. If not set, it will not be used.
    /// </summary>
    [ProtoMember(3)]
    public string? EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the optional event stream identifier to scope to. If not set, it will not be used.
    /// </summary>
    [ProtoMember(4)]
    public string? EventStreamId { get; set; }

    /// <summary>
    /// Gets or sets the optional event source type to scope to. If not set, it will not be used.
    /// </summary>
    [ProtoMember(5)]
    public string? EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the optional collection of event types to scope to. If not set, it will not be used.
    /// </summary>
    [ProtoMember(6)]
    public IList<EventType>? EventTypes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the scope expects no event matching its narrowing to exist yet.
    /// </summary>
    /// <remarks>
    /// The intent lives in a field of its own rather than in a distinguished <see cref="SequenceNumber"/> so that
    /// a version mismatch cannot turn into a check that silently passes. A kernel that predates this field never
    /// reads it, sees the "unavailable" sequence number the scope carries, and skips the check with the warning it
    /// has always emitted - the older behavior, honestly reported - rather than comparing a real tail against a
    /// number nothing can exceed.
    /// </remarks>
    [ProtoMember(7)]
    public bool ExpectsNoMatchingEvent { get; set; }
}
