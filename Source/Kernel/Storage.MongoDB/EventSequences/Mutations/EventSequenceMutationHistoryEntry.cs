// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations;

/// <summary>
/// Represents the MongoDB document containing the terminal receipt for an event sequence mutation.
/// </summary>
/// <param name="MutationId">The unique mutation identifier.</param>
/// <param name="EventSequenceId">The event sequence identifier.</param>
/// <param name="Ordinal">The ordinal assigned to the mutation.</param>
/// <param name="Kind">The kind of mutation.</param>
/// <param name="Target">The event sequence range targeted by the mutation.</param>
/// <param name="TerminalPhase">The terminal phase the mutation reached.</param>
/// <param name="RepairState">The terminal repair state of the mutation.</param>
/// <param name="TerminalWitness">The terminal cryptographic witness.</param>
/// <param name="ArchivedAt">When the mutation was archived, as Unix milliseconds.</param>
public sealed record EventSequenceMutationHistoryEntry(
    EventSequenceMutationId MutationId,
    EventSequenceId EventSequenceId,
    EventSequenceMutationOrdinal Ordinal,
    EventSequenceMutationKind Kind,
    EventSequenceMutationTarget Target,
    EventSequenceMutationPhase TerminalPhase,
    EventSequenceMutationRepairState RepairState,
    EventSequenceMutationTerminalWitness TerminalWitness,
    long ArchivedAt);
