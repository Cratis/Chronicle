// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Checks prepared receipt invariants before the provider mutates any durable state.
/// </summary>
public static class EffectReceiptValidation
{
    /// <summary>
    /// Validates the receipt for this atomic batch. Existing receipts are settled before calling this.
    /// </summary>
    /// <param name="batch">The protected batch.</param>
    /// <param name="receipt">The prepared receipt.</param>
    /// <param name="sequence">The destination sequence.</param>
    /// <exception cref="InvalidEffectReceipt">The receipt cannot describe this write.</exception>
    public static void Validate(IReadOnlyList<EventToAppendToStorage> batch, EffectReceipt receipt, EventSequenceId sequence)
    {
        var hasEvents = batch.Count > 0;
        var appended = receipt.Outcome == EffectOutcome.Appended;
        if (string.IsNullOrEmpty(receipt.Id) || receipt.Outcome is not (EffectOutcome.Appended or EffectOutcome.NoEvents or EffectOutcome.Refused) || appended != hasEvents)
        {
            throw new InvalidEffectReceipt("The receipt identity and outcome must describe the batch.");
        }

        if (batch.Count > 0 && (receipt.EventSequenceId != sequence || receipt.FirstSequenceNumber != batch.Min(item => item.SequenceNumber) || receipt.LastSequenceNumber != batch.Max(item => item.SequenceNumber)))
        {
            throw new InvalidEffectReceipt("The appended receipt must name the batch's destination and sequence range.");
        }

        if (receipt.Response is not null)
        {
            if (Encoding.UTF8.GetByteCount(receipt.Response) > 64 * 1024)
            {
                throw new InvalidEffectReceipt("The receipt response exceeds 64 KiB.");
            }

            try
            {
                using var document = JsonDocument.Parse(receipt.Response);
            }
            catch (JsonException)
            {
                throw new InvalidEffectReceipt("The receipt response must be a JSON value.");
            }
        }
    }
}
