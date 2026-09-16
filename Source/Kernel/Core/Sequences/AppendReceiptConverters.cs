// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts storage-acknowledged append receipts to the generated wire representation.
/// </summary>
internal static class AppendReceiptConverters
{
    /// <summary>
    /// Converts an append receipt without applying client or transport defaults.
    /// </summary>
    /// <param name="receipt">The authoritative receipt.</param>
    /// <returns>The receipt sent to the client.</returns>
    public static Contracts.EventSequences.AppendReceipt ToContract(this EventSequences.AppendReceipt receipt) => new()
    {
        EventStore = receipt.EventStore,
        Namespace = receipt.Namespace,
        SequenceNumber = receipt.SequenceNumber,
        EventTypeId = receipt.EventTypeId,
        Generation = receipt.Generation,
        Tombstone = receipt.Tombstone,
        EventSourceType = receipt.EventSourceType,
        EventSourceId = receipt.EventSourceId,
        EventStreamType = receipt.EventStreamType,
        EventStreamId = receipt.EventStreamId,
        Occurred = receipt.Occurred,
        CorrelationId = receipt.CorrelationId,
        Subject = receipt.Subject,
        Tags = receipt.Tags.Select(tag => tag.Value).ToArray(),
        Hash = receipt.Hash,
        Causation = receipt.Causation.Select(causation => causation.ToContract()).ToArray(),
        CausedBy = receipt.CausedBy.ToContract(),
        ObservationState = (Contracts.Events.EventObservationState)receipt.ObservationState
    };

    /// <summary>
    /// Converts a persisted causation entry.
    /// </summary>
    /// <param name="causation">The persisted cause.</param>
    /// <returns>The wire representation.</returns>
    public static Contracts.EventSequences.AppendCausation ToContract(this EventSequences.AppendCausation causation) => new()
    {
        Occurred = causation.Occurred,
        Type = causation.Type,
        Properties = new Dictionary<string, string>(causation.Properties)
    };

    /// <summary>
    /// Converts the persisted identity chain.
    /// </summary>
    /// <param name="identity">The persisted identity.</param>
    /// <returns>The wire representation.</returns>
    public static Contracts.EventSequences.AppendIdentity ToContract(this EventSequences.AppendIdentity identity) => new()
    {
        Subject = identity.Subject,
        Name = identity.Name,
        UserName = identity.UserName,
        OnBehalfOf = identity.OnBehalfOf?.ToContract()
    };
}
