// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the payload when observing an event sequence.
/// </summary>
[ProtoContract]
public class EventsToObserve
{
    /// <summary>
    /// Gets or sets the partition to observe.
    /// </summary>
    [ProtoMember(1)]
    public string Partition { get; set; }

    /// <summary>
    /// Gets or sets a collection of <see cref="AppendedEvent"/>.
    /// </summary>
    [ProtoMember(2)]
    public IList<AppendedEvent> Events { get; set; } = [];

    /// <summary>
    /// Gets or sets the replay state for this message.
    /// </summary>
    [ProtoMember(3)]
    public ReplayState ReplayState { get; set; }

    /// <summary>
    /// Gets or sets the stream-local delivery identifier to echo in the result. Zero identifies a legacy delivery.
    /// </summary>
    [ProtoMember(4)]
    public ulong DeliveryId { get; set; }

    /// <summary>
    /// Gets or sets how many milliseconds, counted from when the client receives the message, the kernel waits for its
    /// result. Zero means the kernel waits without a deadline.
    /// </summary>
    /// <remarks>
    /// A client that only gets to the message after this has passed can answer it as failed straight away: the kernel
    /// has already given up on it and redelivers the events on the partition's next attempt. It is relative rather than
    /// a point in time so the client's clock does not have to agree with the kernel's.
    /// </remarks>
    [ProtoMember(5)]
    public ulong DeliverWithinMilliseconds { get; set; }
}
