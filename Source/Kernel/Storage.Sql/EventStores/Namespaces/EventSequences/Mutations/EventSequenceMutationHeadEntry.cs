// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations;

/// <summary>
/// Represents the SQL persistence entity for an event sequence mutation head.
/// </summary>
public class EventSequenceMutationHeadEntry
{
    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the mutation coverage.
    /// </summary>
    public EventSequenceMutationCoverage Coverage { get; set; }

    /// <summary>
    /// Gets or sets the last ordinal assigned to a mutation.
    /// </summary>
    public EventSequenceMutationOrdinal LastAssignedOrdinal { get; set; } = EventSequenceMutationOrdinal.NotSet;

    /// <summary>
    /// Gets or sets the active mutation identifier.
    /// </summary>
    public EventSequenceMutationId? ActiveMutationId { get; set; }

    /// <summary>
    /// Gets or sets the active mutation ordinal.
    /// </summary>
    public EventSequenceMutationOrdinal? ActiveOrdinal { get; set; }

    /// <summary>
    /// Gets or sets the event sequence containing the event that originated the active mutation.
    /// </summary>
    public EventSequenceId? ActiveOriginSequence { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the event that originated the active mutation.
    /// </summary>
    public EventSequenceNumber? ActiveOriginSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the active mutation kind.
    /// </summary>
    public EventSequenceMutationKind? ActiveKind { get; set; }

    /// <summary>
    /// Gets or sets the serialized active mutation command payload.
    /// </summary>
    public string? ActiveCommandPayload { get; set; }

    /// <summary>
    /// Gets or sets the active mutation command hash.
    /// </summary>
    public EventSequenceMutationCommandHash? ActiveCommandHash { get; set; }

    /// <summary>
    /// Gets or sets the inclusive start of the active mutation target.
    /// </summary>
    public EventSequenceNumber? ActiveTargetStart { get; set; }

    /// <summary>
    /// Gets or sets the exclusive end of the active mutation target.
    /// </summary>
    public EventSequenceNumber? ActiveTargetEndExclusive { get; set; }

    /// <summary>
    /// Gets or sets the expected event count in the active mutation target.
    /// </summary>
    public EventCount? ActiveTargetExpectedCount { get; set; }

    /// <summary>
    /// Gets or sets the active mutation phase.
    /// </summary>
    public EventSequenceMutationPhase? ActivePhase { get; set; }

    /// <summary>
    /// Gets or sets the phase from which the active mutation became blocked.
    /// </summary>
    public EventSequenceMutationPhase? ActiveBlockedFrom { get; set; }

    /// <summary>
    /// Gets or sets the active mutation repair state.
    /// </summary>
    public EventSequenceMutationRepairState? ActiveRepairState { get; set; }
}
