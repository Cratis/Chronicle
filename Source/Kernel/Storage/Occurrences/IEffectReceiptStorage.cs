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
    /// Reads the durable receipts for an occurrence across destination sequences.
    /// </summary>
    /// <param name="occurrenceId">The occurrence identity.</param>
    /// <returns>The recorded effects.</returns>
    /// <exception cref="OccurrencesStorageNotSupported">The provider has not implemented receipt enumeration.</exception>
    Task<IEnumerable<EffectReceipt>> GetEffectReceipts(OccurrenceId occurrenceId) => throw new OccurrencesStorageNotSupported();

    /// <summary>
    /// Reads receipts for a set of occurrences in one namespace query across destination sequences.
    /// </summary>
    /// <param name="occurrenceIds">The occurrence identities in view.</param>
    /// <returns>The recorded effects.</returns>
    /// <exception cref="OccurrencesStorageNotSupported">The provider has not implemented batched receipt enumeration.</exception>
    Task<IEnumerable<EffectReceipt>> GetEffectReceiptsForOccurrences(IEnumerable<OccurrenceId> occurrenceIds) => throw new OccurrencesStorageNotSupported();

    /// <summary>
    /// Writes the batch and its receipt atomically. A duplicate receipt writes no events; a transaction conflict is retryable. Unknown commit outcomes propagate for settlement by lookup.
    /// </summary>
    /// <param name="events">The fully validated, protected events; may be empty.</param>
    /// <param name="receipt">The receipt to persist.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(IEnumerable<Cratis.Chronicle.Storage.EventSequences.EventToAppendToStorage> events, EffectReceipt receipt);
}
