// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an event and the <see cref="EventSourceId"/> it is for.
/// </summary>
/// <param name="EventSourceId"><see cref="EventSourceId"/> the event is for.</param>
/// <param name="Event">The actual event.</param>
/// <param name="Causation">The causation for the event.</param>
public record EventForEventSourceId(EventSourceId EventSourceId, object Event, Causation Causation)
{
    /// <summary>
    /// Gets or inits the <see cref="EventStreamType"/> for the event. Defaults to <see cref="EventStreamType.All"/>.
    /// </summary>
    public EventStreamType EventStreamType { get; init; } = EventStreamType.All;

    /// <summary>
    /// Gets or inits the <see cref="EventStreamId"/> for the event. Defaults to <see cref="EventStreamId.Default"/>.
    /// </summary>
    public EventStreamId EventStreamId { get; init; } = EventStreamId.Default;
}
