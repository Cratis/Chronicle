// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Exception that gets thrown when an event is missing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingEvent"/> class.
/// </remarks>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> identifying the sequence the event is missing from.</param>
/// <param name="eventTypeId">The <see cref="EventTypeId"/> that is missing.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event missing.</param>
public class MissingEvent(
    EventSequenceId eventSequenceId,
    EventTypeId eventTypeId,
    EventSourceId eventSourceId) : Exception($"Missing event {eventTypeId} for {eventSourceId} in {eventSequenceId}")
{
}
