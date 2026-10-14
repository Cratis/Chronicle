// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Defines direct infrastructure storage for durable application work.
/// </summary>
public interface IEffectReceiptStorage
{
    /// <summary>
    /// Looks up an immutable receipt in namespace scope, independently of the destination sequence.
    /// </summary>
    /// <param name="id">The receipt identity.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<EffectReceipt?> GetEffectReceipt(string id);

    /// <summary>
    /// Writes the batch and its receipt atomically. A duplicate receipt writes no events; a transaction conflict is retryable. Unknown commit outcomes propagate for settlement by lookup.
    /// </summary>
    /// <param name="events">The fully validated, protected events; may be empty.</param>
    /// <param name="receipt">The receipt to persist.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(IEnumerable<Cratis.Chronicle.Storage.EventSequences.EventToAppendToStorage> events, EffectReceipt receipt);
}
