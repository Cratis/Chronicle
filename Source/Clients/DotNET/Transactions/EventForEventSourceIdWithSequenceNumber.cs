// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an event and the <see cref="EventSourceId"/> it is for with a sequence number representing what order it occurred in.
/// </summary>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> for the event.</param>
/// <param name="EventSourceId"><see cref="EventSourceId"/> the event is for.</param>
/// <param name="Event">The actual event.</param>
/// <param name="Causation">The causation for the event.</param>
/// <remarks>
/// This is typically used internally by <see cref="UnitOfWork"/> to keep track of events that are part of the same sequence to guarantee the order of events.
/// </remarks>
public record EventForEventSourceIdWithSequenceNumber(
    EventSequenceNumber SequenceNumber,
    EventSourceId EventSourceId,
    object Event,
    Causation Causation)
{
    /// <summary>
    /// Gets or inits the <see cref="EventStreamType"/> for the event. Defaults to <see cref="EventStreamType.All"/>.
    /// </summary>
    public EventStreamType EventStreamType { get; init; } = EventStreamType.All;

    /// <summary>
    /// Gets or inits the <see cref="EventStreamId"/> for the event. Defaults to <see cref="EventStreamId.Default"/>.
    /// </summary>
    public EventStreamId EventStreamId { get; init; } = EventStreamId.Default;

    /// <summary>
    /// Gets or inits the <see cref="EventSourceType"/> for the event. Defaults to <see cref="EventSourceType.Default"/>.
    /// </summary>
    public EventSourceType EventSourceType { get; init; } = EventSourceType.Default;
}
