// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the persisted state of an event sequence mutation.
/// </summary>
/// <param name="Id">The unique mutation identifier.</param>
/// <param name="Ordinal">The ordinal assigned to the mutation.</param>
/// <param name="Origin">The event that originated the mutation.</param>
/// <param name="Command">The persisted mutation command.</param>
/// <param name="Target">The event sequence range targeted by the mutation.</param>
/// <param name="Phase">The current mutation phase.</param>
/// <param name="BlockedFrom">The phase from which the mutation became blocked.</param>
/// <param name="RepairState">The repair state of the mutation.</param>
public sealed record EventSequenceMutation(
    EventSequenceMutationId Id,
    EventSequenceMutationOrdinal Ordinal,
    EventSequenceMutationOrigin Origin,
    EventSequenceMutationCommandEnvelope Command,
    EventSequenceMutationTarget Target,
    EventSequenceMutationPhase Phase,
    EventSequenceMutationPhase BlockedFrom,
    EventSequenceMutationRepairState RepairState);
