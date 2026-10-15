// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;

using EffectReceipt = Cratis.Chronicle.Occurrences.EffectReceipt;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    static string ReceiptId(EffectReceipt receipt) => OccurrenceIdentities.ForEffect(new(receipt.OccurrenceId), receipt.EffectKey);

    static void ValidateEffectReceipt(EffectReceipt receipt)
    {
        if (string.IsNullOrEmpty(receipt.OccurrenceId) || string.IsNullOrWhiteSpace(receipt.EffectKey))
        {
            throw new InvalidEffectReceipt("The occurrence identity and effect key must be nonempty.");
        }

        if (receipt.Response is null) return;
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
