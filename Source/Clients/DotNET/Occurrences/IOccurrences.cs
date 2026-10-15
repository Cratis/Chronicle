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
