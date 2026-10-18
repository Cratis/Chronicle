// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The refusal sentences shared by keyed single and batch appends. Never include protected content.
/// </summary>
internal static class IdempotencyRefusals
{
    /// <summary>
    /// Describes a durable intent conflict.
    /// </summary>
    /// <param name="conflict">The original append and the conflicting generation.</param>
    /// <returns>The coded refusal.</returns>
    internal static AppendError Conflict(IdempotencyConflict conflict)
    {
        var sentence = conflict.Reason == IdempotencyConflictReason.Generation
            ? $"Idempotency key '{conflict.Key.Value}' already appended event type '{conflict.StoredEventType.Id}' generation {conflict.StoredEventType.Generation} at sequence number {conflict.ExistingSequenceNumber}; this append is generation {conflict.RequestedGeneration}. Keyed appends are not migrated between generations. To repeat the recorded effect, append generation {conflict.StoredEventType.Generation}; for a new effect, use a new idempotency key. Nothing was written."
            : $"Idempotency key '{conflict.Key.Value}' already appended event type '{conflict.StoredEventType.Id}' generation {conflict.StoredEventType.Generation} at sequence number {conflict.ExistingSequenceNumber} with a different event source, route or content. Nothing was written.";
        return Describe("IdempotencyKeyConflict", sentence);
    }

    /// <summary>
    /// Describes a protected part whose key is absent without erasure evidence.
    /// </summary>
    /// <param name="key">The append key.</param>
    /// <param name="stored">The original type.</param>
    /// <param name="number">The durable sequence number.</param>
    /// <param name="kind">The first unverifiable protection kind.</param>
    /// <returns>The coded refusal.</returns>
    internal static AppendError Unverifiable(IdempotencyKey key, EventType stored, EventSequenceNumber number, ProtectionKind? kind) => Describe(
        "IdempotencyKeyUnverifiable",
        $"Idempotency key '{key.Value}' already appended event type '{stored.Id}' at sequence number {number}, but the key that protected its {(kind == ProtectionKind.Pii ? "pii" : "encrypted")} values is not in key storage and no erasure covers it, so this retry cannot be compared. Nothing was written. Restore the key from a backup, or, for personal data that is meant to be gone, erase the subject; then recover the partition.");

    /// <summary>
    /// Describes an absent key after the one permitted retry or divergent encrypting keys.
    /// </summary>
    /// <param name="key">The append key.</param>
    /// <returns>The coded refusal.</returns>
    internal static AppendError Unsettled(IdempotencyKey key) => Describe(
        "IdempotencyKeyUnsettled",
        $"Idempotency key '{key.Value}' could not be settled because another append with the same key was still in progress, or the key protecting its values changed during the append. Nothing was written. Retry the append.");

    /// <summary>
    /// Refuses a damaged or unknown fingerprint format without guessing the original type.
    /// </summary>
    /// <param name="key">The append key.</param>
    /// <returns>The coded refusal.</returns>
    internal static AppendError Malformed(IdempotencyKey key) => Describe(
        "IdempotencyKeyUnverifiable",
        $"Idempotency key '{key.Value}' has an unreadable stored fingerprint, so this retry cannot be compared. Nothing was written.");

    /// <summary>
    /// Refuses a boundary the fingerprint format cannot represent.
    /// </summary>
    /// <param name="key">The append key.</param>
    /// <returns>The coded refusal.</returns>
    internal static AppendError Unsupported(IdempotencyKey key) => Describe(
        "IdempotencyFingerprintUnsupported",
        $"Idempotency key '{key.Value}' has a protected boundary that cannot be fingerprinted. Nothing was written.");

    static AppendError Describe(string code, string sentence) => new($"{code}: {sentence}");
}
