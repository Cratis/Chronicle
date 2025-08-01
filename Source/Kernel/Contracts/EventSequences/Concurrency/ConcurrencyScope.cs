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
}
