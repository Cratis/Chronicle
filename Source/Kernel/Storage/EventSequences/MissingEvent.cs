// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Storage.EventSequences;

/// <summary>
/// Exception that gets thrown when an event is missing.
/// </summary>
public class MissingEvent : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEvent"/> class.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> identifying the sequence the event is missing from.</param>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> that is missing.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event missing.</param>
    public MissingEvent(
        EventSequenceId eventSequenceId,
        EventTypeId eventTypeId,
        EventSourceId eventSourceId) : base($"Missing event {eventTypeId} for {eventSourceId} in {eventSequenceId}")
    {
    }
}
