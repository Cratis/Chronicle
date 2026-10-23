// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents a keyed append that was refused because its key is already durable for a different intent.
/// </summary>
/// <param name="Key">The idempotency key.</param>
/// <param name="ExistingSequenceNumber">The sequence number of the event already stored under the key.</param>
/// <param name="StoredEventTypeId">The identifier of the stored event type.</param>
/// <param name="StoredEventTypeGeneration">The generation of the stored event type.</param>
/// <param name="RequestedGeneration">The generation of the event type in the refused request.</param>
/// <param name="Reason">Why the retry is not the recorded intent.</param>
public record IdempotencyKeyConflict(
    string Key,
    ulong ExistingSequenceNumber,
    string StoredEventTypeId,
    uint StoredEventTypeGeneration,
    uint RequestedGeneration,
    IdempotencyConflictReason Reason)
{
    /// <summary>
    /// Converts a kernel <see cref="IdempotencyConflict"/> to its transport representation.
    /// </summary>
    /// <param name="conflict">The <see cref="IdempotencyConflict"/> to convert.</param>
    /// <returns>The <see cref="IdempotencyKeyConflict"/>.</returns>
    internal static IdempotencyKeyConflict From(IdempotencyConflict conflict) => new(
        conflict.Key.Value,
        conflict.ExistingSequenceNumber,
        conflict.StoredEventType.Id,
        conflict.StoredEventType.Generation,
        conflict.RequestedGeneration,
        conflict.Reason);
}
