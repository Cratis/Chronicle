// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// The immutable outcome and response recorded for an occurrence effect.
/// </summary>
/// <param name="Id">The namespace-scoped receipt identity.</param>
/// <param name="OccurrenceId">The occurrence identity.</param>
/// <param name="Epoch">The epoch that wrote the receipt.</param>
/// <param name="Outcome">Appended, NoEvents or Refused.</param>
/// <param name="EventSequenceId">The appended sequence, if any.</param>
/// <param name="FirstSequenceNumber">The first appended slot, if any.</param>
/// <param name="LastSequenceNumber">The last appended slot, if any.</param>
/// <param name="Reasons">The refusal reasons.</param>
/// <param name="Response">The optional verbatim JSON response.</param>
/// <param name="RecordedAt">The kernel's recording instant.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Occurrences)]
public record EffectReceiptRecord(
    string Id,
    string OccurrenceId,
    long Epoch,
    EffectOutcome Outcome,
    string? EventSequenceId,
    ulong? FirstSequenceNumber,
    ulong? LastSequenceNumber,
    IEnumerable<string> Reasons,
    string? Response,
    DateTimeOffset RecordedAt)
{
    /// <summary>
    /// Looks up a receipt in the target event store and namespace without executing the effect.
    /// </summary>
    /// <param name="eventStore">The target event store.</param>
    /// <param name="namespace">The target namespace.</param>
    /// <param name="occurrenceId">The occurrence identity.</param>
    /// <param name="effectKey">The effect name.</param>
    /// <param name="storage">The kernel storage.</param>
    /// <returns>The winning receipt, or null if it is absent.</returns>
    /// <exception cref="OccurrencesStorageNotSupported">The provider does not support atomic effect receipts.</exception>
    public static async Task<EffectReceiptRecord?> GetEffectReceipt(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string occurrenceId,
        string effectKey,
        IStorage storage)
    {
        var sequence = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(Concepts.EventSequences.EventSequenceId.Log);
        var receipts = sequence as IEffectReceiptStorage ?? throw new OccurrencesStorageNotSupported();
        var receipt = await receipts.GetEffectReceipt(OccurrenceIdentities.ForEffect(new OccurrenceId(occurrenceId), effectKey));

        return receipt is null ? null : EffectReceiptConverters.ToRecord(receipt);
    }
}
