// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency scope for an event sequence append operation.
/// </summary>
/// <param name="SequenceNumber">The expected sequence number.</param>
/// <param name="EventSourceId">Optional <see cref="EventSourceId"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventStreamType">Optional <see cref="EventStreamType"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventStreamId">Optional <see cref="EventStreamId"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventSourceType">Optional <see cref="EventSourceType"/> to scope to. If not set, it will not be used.</param>
/// <param name="EventTypes">Optional collection of <see cref="EventType"/> to scope to. If not set, it will not be used.</param>
public record ConcurrencyScope(
    EventSequenceNumber SequenceNumber,
    EventSourceId? EventSourceId,
    EventStreamType? EventStreamType,
    EventStreamId? EventStreamId,
    EventSourceType? EventSourceType,
    IEnumerable<EventType>? EventTypes)
{
    /// <summary>
    /// Represents a concurrency scope that has not been specified yet.
    /// </summary>
    public static readonly ConcurrencyScope NotSet = new(
        EventSequenceNumber.Max,
        default,
        default,
        default,
        default,
        default);

    /// <summary>
    /// Represents a concurrency scope that does not apply any constraints.
    /// </summary>
    public static readonly ConcurrencyScope None = new(
        EventSequenceNumber.Unavailable,
        default,
        default,
        default,
        default,
        default);
}