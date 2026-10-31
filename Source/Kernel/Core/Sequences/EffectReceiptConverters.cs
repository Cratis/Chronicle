// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Makes occurrence receipt conversions available to the generated append service.
/// </summary>
internal static class EffectReceiptConverters
{
    /// <summary>
    /// Converts receipt enrollment from the wire.
    /// </summary>
    /// <param name="receipt">The wire enrollment.</param>
    /// <returns>The kernel enrollment.</returns>
    public static Occurrences.EffectReceipt ToApi(this Contracts.Occurrences.EffectReceipt receipt) => Occurrences.EffectReceiptConverters.ToApi(receipt);

    /// <summary>
    /// Converts the winning receipt to the wire.
    /// </summary>
    /// <param name="receipt">The winning receipt.</param>
    /// <returns>The wire receipt.</returns>
    public static Contracts.Occurrences.EffectReceiptDetails ToContract(this Occurrences.EffectReceiptDetails receipt) => Occurrences.EffectReceiptConverters.ToContract(receipt);
}
