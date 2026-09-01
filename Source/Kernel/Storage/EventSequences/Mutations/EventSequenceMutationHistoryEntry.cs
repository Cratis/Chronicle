// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the terminal receipt persisted for an event sequence mutation.
/// </summary>
/// <param name="Id">The unique mutation identifier.</param>
/// <param name="Ordinal">The ordinal assigned to the mutation.</param>
/// <param name="Origin">The event that originated the mutation.</param>
/// <param name="Kind">The kind of mutation.</param>
/// <param name="CommandHash">The hash of the mutation command.</param>
/// <param name="Target">The event sequence range targeted by the mutation.</param>
/// <param name="RepairState">The terminal repair state of the mutation.</param>
public sealed record EventSequenceMutationHistoryEntry(
    EventSequenceMutationId Id,
    EventSequenceMutationOrdinal Ordinal,
    EventSequenceMutationOrigin Origin,
    EventSequenceMutationKind Kind,
    EventSequenceMutationCommandHash CommandHash,
    EventSequenceMutationTarget Target,
    EventSequenceMutationRepairState RepairState);
