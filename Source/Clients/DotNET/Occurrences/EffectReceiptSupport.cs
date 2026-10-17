// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Validates receipt support and translates receipt contracts without an unreceipted fallback.
/// </summary>
internal static class EffectReceiptSupport
{
    /// <summary>
    /// Requires an explicitly advertised receipt capability.
    /// </summary>
    /// <param name="connection">The current kernel connection.</param>
    /// <exception cref="EffectReceiptsNotSupported">The capability is absent or unknown.</exception>
    internal static void Require(IChronicleConnection connection)
    {
        if (connection is not IKernelCapabilities { CapabilitiesAreKnown: true } capabilities ||
            !capabilities.Capabilities.Contains(KernelCapabilities.EffectReceipts))
        {
            throw new EffectReceiptsNotSupported();
        }
    }

    /// <summary>
    /// Validates and snapshots the effect response for an atomic write.
    /// </summary>
    /// <param name="key">The effect identity and lease.</param>
    /// <param name="response">The optional response.</param>
    /// <returns>The immutable wire snapshot.</returns>
    /// <exception cref="EffectReceiptNotValid">The key or lease is invalid.</exception>
    /// <exception cref="EffectReceiptResponseTooLarge">The serialized response exceeds 64 KiB.</exception>
    internal static Contracts.Occurrences.EffectReceipt ToContract(EffectKey key, JsonNode? response)
    {
        if (string.IsNullOrWhiteSpace(key.Occurrence.Value) || string.IsNullOrWhiteSpace(key.Name.Value) || key.WrittenBy.OccurrenceId != key.Occurrence)
        {
            throw new EffectReceiptNotValid("The occurrence and effect name must be specified and the writing lease must identify that occurrence.");
        }
        if (key.WrittenBy.Epoch < 0) throw new EffectReceiptNotValid("The lease epoch must not be negative.");
        var json = response?.ToJsonString();
        if (json is not null && Encoding.UTF8.GetByteCount(json) > 64 * 1024)
        {
            throw new EffectReceiptResponseTooLarge();
        }

        return new()
        {
            OccurrenceId = key.Occurrence,
            EffectKey = key.Name,
            Epoch = key.WrittenBy.Epoch,
            Response = json
        };
    }

    /// <summary>
    /// Translates the winning append receipt.
    /// </summary>
    /// <param name="key">The requested effect.</param>
    /// <param name="receipt">The stored receipt.</param>
    /// <returns>The winning receipt.</returns>
    internal static EffectReceipt ToClient(EffectKey key, Contracts.Occurrences.EffectReceiptDetails receipt) =>
        ToClient(key, receipt.Id, receipt.OccurrenceId, receipt.Epoch, receipt.Outcome, receipt.EventSequenceId, receipt.FirstSequenceNumber, receipt.LastSequenceNumber, receipt.Reasons, receipt.Response, receipt.RecordedAt);

    /// <summary>
    /// Translates a lookup or receipt-only write.
    /// </summary>
    /// <param name="key">The requested effect.</param>
    /// <param name="receipt">The stored receipt.</param>
    /// <returns>The winning receipt.</returns>
    internal static EffectReceipt ToClient(EffectKey key, Contracts.Occurrences.EffectReceiptRecordResponse receipt) =>
        ToClient(key, receipt.Id, receipt.OccurrenceId, receipt.Epoch, receipt.Outcome, receipt.EventSequenceId, receipt.FirstSequenceNumber, receipt.LastSequenceNumber, receipt.Reasons, receipt.Response, receipt.RecordedAt);

    /// <summary>
    /// Resolves a receipted append, requiring its receipt on success.
    /// </summary>
    /// <param name="request">The submitted effect.</param>
    /// <param name="response">The append result.</param>
    /// <returns>The winning receipt, or no receipt for a refused append.</returns>
    /// <exception cref="EffectReceiptNotValid">A successful append omitted its receipt.</exception>
    internal static EffectReceiptResult? FromAppend(Contracts.Occurrences.EffectReceipt request, Contracts.Sequences.AppendManyResponse response)
    {
        if (response.EffectReceipt is null)
        {
            if (response.IsSuccess) throw new EffectReceiptNotValid("A successful atomic append did not return its receipt.");
            return null;
        }
        var occurrence = new OccurrenceId(request.OccurrenceId);
        var key = new EffectKey(occurrence, request.EffectKey, new(occurrence, request.Epoch));

        return new(response.EffectAlreadyRecorded ? EffectReceiptStatus.AlreadyRecorded : EffectReceiptStatus.Recorded, ToClient(key, response.EffectReceipt));
    }

    static EffectReceipt ToClient(
        EffectKey key,
        string id,
        string occurrence,
        long epoch,
        Contracts.Storage.Occurrences.EffectOutcome outcome,
        string? sequence,
        ulong? first,
        ulong? last,
        IEnumerable<string> reasons,
        string? response,
        DateTimeOffset recordedAt)
    {
        if (id != key.Value || occurrence != key.Occurrence.Value || epoch < 0)
        {
            throw new EffectReceiptNotValid("The returned receipt does not identify the requested effect.");
        }
        EffectOutcome result = outcome switch
        {
            Contracts.Storage.Occurrences.EffectOutcome.Appended when sequence is not null && first is not null && last is not null => new Appended(sequence, first.Value, last.Value),
            Contracts.Storage.Occurrences.EffectOutcome.NoEvents => new NoEvents(),
            Contracts.Storage.Occurrences.EffectOutcome.Refused => new Refused(reasons.ToArray()),
            _ => throw new EffectReceiptNotValid("The kernel returned an unknown or incomplete outcome.")
        };
        var lease = new LeaseToken(occurrence, epoch);

        return new(key with { WrittenBy = lease }, result, response is null ? null : JsonNode.Parse(response), lease, recordedAt);
    }
}
