// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations;

/// <summary>
/// Represents the MongoDB document containing the mutation head for an event sequence.
/// </summary>
/// <param name="EventSequenceId">The event sequence identifier.</param>
/// <param name="Coverage">The persisted mutation coverage.</param>
/// <param name="LastAssignedOrdinal">The last ordinal assigned to a mutation.</param>
/// <param name="Active">The active mutation, or <see langword="null"/> when no mutation is active.</param>
public sealed record EventSequenceMutationHeadEntry(
    EventSequenceId EventSequenceId,
    EventSequenceMutationCoverage Coverage,
    EventSequenceMutationOrdinal LastAssignedOrdinal,
    EventSequenceMutation? Active);
