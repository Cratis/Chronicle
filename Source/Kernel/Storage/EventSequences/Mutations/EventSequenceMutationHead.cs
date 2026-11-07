// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the mutation head for an event sequence.
/// </summary>
/// <param name="Coverage">The persisted mutation coverage.</param>
/// <param name="LastAssignedOrdinal">The last ordinal assigned to a mutation.</param>
/// <param name="Active">The active mutation, or <see langword="null"/> when no mutation is active.</param>
public sealed record EventSequenceMutationHead(
    EventSequenceMutationCoverage Coverage,
    EventSequenceMutationOrdinal LastAssignedOrdinal,
    EventSequenceMutation? Active)
{
    /// <summary>
    /// Gets the initial mutation head for an event sequence.
    /// </summary>
    public static readonly EventSequenceMutationHead Initial = new(EventSequenceMutationCoverage.Untracked, EventSequenceMutationOrdinal.NotSet, null);
}
