// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Converts between the import wire contracts and their Core artifacts.
/// </summary>
public static class ImportContractConverters
{
    /// <summary>
    /// Converts a contract import event to its Core artifact.
    /// </summary>
    /// <param name="event">The <see cref="Contracts.Imports.ImportEvent"/>.</param>
    /// <returns>The <see cref="ImportEvent"/>.</returns>
    public static ImportEvent ToApi(this Contracts.Imports.ImportEvent @event) => new(
        @event.EventSourceId,
        @event.EventSourceType,
        @event.EventStreamType,
        @event.EventStreamId,
        @event.EventType.ToApi(),
        @event.Content,
        @event.Tags?.ToArray(),
        @event.NamedTags?.Select(_ => _.ToApi()).ToArray(),
        @event.Occurred,
        @event.Subject,
        @event.CorrelationId,
        @event.Causation?.ToApi().ToArray(),
        @event.CausedBy?.ToApi(),
        @event.IdempotencyKey,
        @event.EventSource,
        @event.SourceSequenceNumber);

    /// <summary>
    /// Converts a rejection to its contract.
    /// </summary>
    /// <param name="rejection">The <see cref="ImportEventRejection"/>.</param>
    /// <returns>The <see cref="Contracts.Imports.ImportEventRejection"/>.</returns>
    public static Contracts.Imports.ImportEventRejection ToContract(this ImportEventRejection rejection) => new()
    {
        Index = rejection.Index,
        EventType = rejection.EventType,
        Reasons = rejection.Reasons.ToList()
    };

    /// <summary>
    /// Converts a committed range to its contract.
    /// </summary>
    /// <param name="range">The <see cref="ImportSequenceRange"/>.</param>
    /// <returns>The <see cref="Contracts.Imports.ImportSequenceRange"/>.</returns>
    public static Contracts.Imports.ImportSequenceRange ToContract(this ImportSequenceRange range) => new()
    {
        ChunkIndex = range.ChunkIndex,
        First = range.First,
        Last = range.Last
    };
}
