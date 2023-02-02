// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents the state for the catch up process of an observer.
/// </summary>
public class CatchUpState
{
    /// <summary>
    /// Gets or sets the <see cref="EventSequenceId"/> the state is for.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the expected next event sequence number into the event log.
    /// </summary>
    public EventSequenceNumber NextEventSequenceNumber { get; set; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the event types the observer is observing.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; } = Array.Empty<EventType>();
}
