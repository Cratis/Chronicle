// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Provides namespace-bound occurrence effect receipts.
/// </summary>
public interface IOccurrences
{
    /// <summary>
    /// Records an application trigger in this namespace. This is an in-process API, not a public trigger endpoint.
    /// </summary>
    /// <param name="trigger">The discovered typed plaintext payload, without personal data.</param>
    /// <param name="idempotencyId">A stable identity for this ingress, reused on retries.</param>
    /// <returns>The durable ingress outcome, including identity conflicts and invalid payloads.</returns>
    /// <exception cref="OccurrencesNotSupported">Occurrence support is absent or unknown.</exception>
    /// <exception cref="TriggerNotValid">The trigger has not been discovered.</exception>
    Task<OccurrenceRecordResult> Record(object trigger, string idempotencyId);

    /// <summary>
    /// Explicitly ends a logical activation, recording Shutdown once. Stopping one instance never calls this implicitly.
    /// </summary>
    /// <param name="activationId">The activation to end, shared across all instances.</param>
    /// <returns>The durable recording work.</returns>
    /// <exception cref="OccurrencesNotSupported">Occurrence support is absent or unknown.</exception>
    Task EndActivation(ActivationId activationId) => throw new OccurrencesNotSupported();

    /// <summary>
    /// Looks up the first durable outcome without executing the effect again.
    /// </summary>
    /// <param name="key">The effect key.</param>
    /// <returns>The stored receipt, or null if none exists.</returns>
    /// <exception cref="EffectReceiptsNotSupported">Receipt support is absent or unknown.</exception>
    Task<EffectReceipt?> GetEffectReceipt(EffectKey key);

    /// <summary>
    /// Records a terminal refusal or a successful effect without events or protected reads.
    /// </summary>
    /// <param name="key">The effect key.</param>
    /// <param name="outcome">NoEvents or Refused. Appended is only written through a unit of work.</param>
    /// <param name="response">Optional plaintext JSON response, without personal data.</param>
    /// <returns>The first durable receipt, including its winning response.</returns>
    /// <exception cref="EffectReceiptsNotSupported">Receipt support is absent or unknown.</exception>
    /// <exception cref="EffectReceiptResponseTooLarge">The response exceeds 64 KiB of UTF-8 JSON.</exception>
    /// <exception cref="EffectReceiptNotValid">The key or outcome is invalid.</exception>
    Task<EffectReceiptResult> RecordEffectReceipt(EffectKey key, EffectOutcome outcome, JsonNode? response = null);
}
