// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Converts namespace-qualified status receipts to their generated wire messages.
/// </summary>
internal static class OccurrenceEffectReceiptConverters
{
    /// <summary>
    /// Preserves both the namespace and the recorded effect on the wire.
    /// </summary>
    /// <param name="entry">The namespace-qualified effect.</param>
    /// <returns>The wire status entry.</returns>
    public static Contracts.Occurrences.OccurrenceEffectReceipt ToContract(this OccurrenceEffectReceipt entry) => new()
    {
        Namespace = entry.Namespace,
        Receipt = entry.Receipt.ToContract()
    };
}
