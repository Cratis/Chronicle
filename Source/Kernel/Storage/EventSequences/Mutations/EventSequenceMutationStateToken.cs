// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Binds the complete state required to fence an event sequence mutation transition.
/// </summary>
public sealed record EventSequenceMutationStateToken
{
    EventSequenceMutationStateToken(EventSequenceKey scope, EventSequenceMutation mutation)
    {
        Scope = scope;
        TargetKey = mutation.TargetSequence.Key;
        Id = mutation.Id;
        Ordinal = mutation.Ordinal;
        DefinitionDigestV1 = mutation.Definition.DefinitionDigestV1;
        Phase = mutation.Phase;
        BlockedFrom = mutation.BlockedFrom;
        RepairState = mutation.RepairState;
        StateVersion = mutation.StateVersion;
    }

    /// <summary>Gets the full event sequence scope.</summary>
    public EventSequenceKey Scope { get; }

    /// <summary>Gets the canonical target identity key.</summary>
    public EventSequenceIdentityKey TargetKey { get; }

    /// <summary>Gets the mutation identifier.</summary>
    public EventSequenceMutationId Id { get; }

    /// <summary>Gets the mutation ordinal.</summary>
    public EventSequenceMutationOrdinal Ordinal { get; }

    /// <summary>Gets the definition digest.</summary>
    public EventSequenceMutationDefinitionDigestV1 DefinitionDigestV1 { get; }

    /// <summary>Gets the phase.</summary>
    public EventSequenceMutationPhase Phase { get; }

    /// <summary>Gets the blocked source phase.</summary>
    public EventSequenceMutationPhase BlockedFrom { get; }

    /// <summary>Gets the repair state.</summary>
    public EventSequenceMutationRepairState RepairState { get; }

    /// <summary>Gets the state version.</summary>
    public EventSequenceMutationStateVersion StateVersion { get; }

    /// <summary>
    /// Creates a token from a validated active mutation.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="mutation">The active mutation.</param>
    /// <returns>The full state token.</returns>
    /// <exception cref="InvalidEventSequenceMutation">Thrown when the active mutation is malformed.</exception>
    public static EventSequenceMutationStateToken Create(EventSequenceKey scope, EventSequenceMutation mutation)
    {
        EventSequenceMutationValidator.ValidateActive(scope, mutation).ThrowIfInvalid();
        return new(scope, mutation);
    }
}
