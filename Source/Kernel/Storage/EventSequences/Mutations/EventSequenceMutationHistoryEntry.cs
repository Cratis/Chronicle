// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the terminal receipt persisted for an event sequence mutation.
/// </summary>
/// <param name="Id">The unique mutation identifier.</param>
/// <param name="Ordinal">The ordinal assigned to the mutation.</param>
/// <param name="Kind">The kind of mutation.</param>
/// <param name="Target">The event sequence range targeted by the mutation.</param>
/// <param name="TerminalPhase">The terminal phase the mutation reached; always <see cref="EventSequenceMutationPhase.SourceCommitted"/>.</param>
/// <param name="RepairState">The terminal repair state of the mutation.</param>
/// <param name="TerminalWitness">The terminal cryptographic witness.</param>
/// <param name="ArchivedAt">When the mutation was archived, in UTC with millisecond precision.</param>
public sealed record EventSequenceMutationHistoryEntry(
    EventSequenceMutationId Id,
    EventSequenceMutationOrdinal Ordinal,
    EventSequenceMutationKind Kind,
    EventSequenceMutationTarget Target,
    EventSequenceMutationPhase TerminalPhase,
    EventSequenceMutationRepairState RepairState,
    EventSequenceMutationTerminalWitness TerminalWitness,
    DateTimeOffset ArchivedAt);
