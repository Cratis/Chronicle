// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Converts the stored winning outcome to the kernel read surface.
/// </summary>
internal static class EffectReceiptConverters
{
    /// <summary>
    /// Converts receipt enrollment from its generated wire representation.
    /// </summary>
    /// <param name="receipt">The wire enrollment.</param>
    /// <returns>The kernel enrollment.</returns>
    public static EffectReceipt ToApi(this Contracts.Occurrences.EffectReceipt receipt) => new(receipt.OccurrenceId, receipt.EffectKey, receipt.Epoch, receipt.Response);

    /// <summary>
    /// Converts the winning receipt to its generated wire representation.
    /// </summary>
    /// <param name="receipt">The winning receipt.</param>
    /// <returns>The wire receipt.</returns>
    public static Contracts.Occurrences.EffectReceiptDetails ToContract(this EffectReceiptDetails receipt) => new()
    {
        Id = receipt.Id,
        OccurrenceId = receipt.OccurrenceId,
        Epoch = receipt.Epoch,
        Outcome = (Contracts.Storage.Occurrences.EffectOutcome)receipt.Outcome,
        EventSequenceId = receipt.EventSequenceId,
        FirstSequenceNumber = receipt.FirstSequenceNumber,
        LastSequenceNumber = receipt.LastSequenceNumber,
        Reasons = receipt.Reasons,
        Response = receipt.Response,
        RecordedAt = receipt.RecordedAt
    };

    /// <summary>
    /// Converts a durable receipt without changing its opaque response.
    /// </summary>
    /// <param name="receipt">The stored receipt.</param>
    /// <returns>The receipt read surface.</returns>
    internal static EffectReceiptDetails ToDetails(Storage.Occurrences.EffectReceipt receipt) => new(
        receipt.Id,
        receipt.Lease.OccurrenceId.Value,
        receipt.Lease.Epoch,
        receipt.Outcome,
        receipt.EventSequenceId?.Value,
        receipt.FirstSequenceNumber?.Value,
        receipt.LastSequenceNumber?.Value,
        receipt.Reasons,
        receipt.Response,
        receipt.RecordedAt);

    /// <summary>
    /// Converts a durable receipt to the flattened lookup read model.
    /// </summary>
    /// <param name="receipt">The stored receipt.</param>
    /// <returns>The receipt lookup.</returns>
    internal static EffectReceiptRecord ToRecord(Storage.Occurrences.EffectReceipt receipt) => new(
        receipt.Id,
        receipt.Lease.OccurrenceId.Value,
        receipt.Lease.Epoch,
        receipt.Outcome,
        receipt.EventSequenceId?.Value,
        receipt.FirstSequenceNumber?.Value,
        receipt.LastSequenceNumber?.Value,
        receipt.Reasons,
        receipt.Response,
        receipt.RecordedAt);
}
