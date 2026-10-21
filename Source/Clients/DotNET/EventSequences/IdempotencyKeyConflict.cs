// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an idempotency key that was refused because it already identifies a different event.
/// </summary>
/// <param name="Key">The key that was refused.</param>
/// <param name="ExistingSequenceNumber">The sequence number of the event that holds the key.</param>
/// <param name="StoredEventType">The event type, with generation, of the stored event.</param>
/// <param name="RequestedGeneration">The generation of the event type that was requested.</param>
/// <param name="Reason">Why the key was refused.</param>
public record IdempotencyKeyConflict(
    IdempotencyKey Key,
    EventSequenceNumber ExistingSequenceNumber,
    EventType StoredEventType,
    EventTypeGeneration RequestedGeneration,
    IdempotencyConflictReason Reason);
