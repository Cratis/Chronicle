// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations;

/// <summary>
/// Represents the SQL persistence entity for a terminal event sequence mutation receipt.
/// </summary>
public class EventSequenceMutationHistoryEntry
{
    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the mutation ordinal.
    /// </summary>
    public EventSequenceMutationOrdinal Ordinal { get; set; } = EventSequenceMutationOrdinal.NotSet;

    /// <summary>
    /// Gets or sets the unique mutation identifier.
    /// </summary>
    public EventSequenceMutationId MutationId { get; set; } = EventSequenceMutationId.NotSet;

    /// <summary>
    /// Gets or sets the event sequence containing the event that originated the mutation.
    /// </summary>
    public EventSequenceId OriginSequence { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the sequence number of the event that originated the mutation.
    /// </summary>
    public EventSequenceNumber OriginSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the mutation kind.
    /// </summary>
    public EventSequenceMutationKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the mutation command hash.
    /// </summary>
    public EventSequenceMutationCommandHash CommandHash { get; set; } = EventSequenceMutationCommandHash.NotSet;

    /// <summary>
    /// Gets or sets the inclusive start of the mutation target.
    /// </summary>
    public EventSequenceNumber TargetStart { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the exclusive end of the mutation target.
    /// </summary>
    public EventSequenceNumber TargetEndExclusive { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the expected event count in the mutation target.
    /// </summary>
    public EventCount TargetExpectedCount { get; set; } = EventCount.NotSet;

    /// <summary>
    /// Gets or sets the terminal repair state.
    /// </summary>
    public EventSequenceMutationRepairState RepairState { get; set; }
}
