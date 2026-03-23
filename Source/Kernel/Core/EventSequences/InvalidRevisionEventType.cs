// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when attempting to revise an event with a different event type.
/// </summary>
/// <param name="sequenceNumber">The sequence number of the event being revised.</param>
/// <param name="originalEventTypeId">The original event type identifier.</param>
/// <param name="revisionEventTypeId">The revision event type identifier that was attempted.</param>
public class InvalidRevisionEventType(EventSequenceNumber sequenceNumber, EventTypeId originalEventTypeId, EventTypeId revisionEventTypeId)
    : Exception($"Cannot revise event at sequence number '{sequenceNumber}'. Original event type is '{originalEventTypeId}', but revision event type '{revisionEventTypeId}' was provided. Event types must match.");
