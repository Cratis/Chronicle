// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// The exception that is thrown when attempting to compensate an event with a different event type.
/// </summary>
/// <param name="sequenceNumber">The sequence number of the event being compensated.</param>
/// <param name="originalEventTypeId">The original event type identifier.</param>
/// <param name="compensationEventTypeId">The compensation event type identifier that was attempted.</param>
public class InvalidCompensationEventType(EventSequenceNumber sequenceNumber, EventTypeId originalEventTypeId, EventTypeId compensationEventTypeId)
    : Exception($"Cannot compensate event at sequence number '{sequenceNumber}'. Original event type is '{originalEventTypeId}', but compensation event type '{compensationEventTypeId}' was provided. Event types must match.");
