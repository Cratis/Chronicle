// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the persisted active state of an event sequence mutation.
/// </summary>
/// <param name="Definition">The registered mutation definition.</param>
/// <param name="Ordinal">The positive ordinal assigned to the mutation.</param>
/// <param name="StateVersion">The positive state version.</param>
/// <param name="Phase">The current mutation phase.</param>
/// <param name="BlockedFrom">The phase from which the mutation became blocked.</param>
/// <param name="RepairState">The repair state of the mutation.</param>
public sealed record EventSequenceMutation(
    EventSequenceMutationDefinition Definition,
    EventSequenceMutationOrdinal Ordinal,
    EventSequenceMutationStateVersion StateVersion,
    EventSequenceMutationPhase Phase,
    EventSequenceMutationPhase BlockedFrom,
    EventSequenceMutationRepairState RepairState)
{
    /// <summary>
    /// Gets the mutation identifier.
    /// </summary>
    public EventSequenceMutationId Id => Definition.Request.Id;

    /// <summary>
    /// Gets the target sequence identity.
    /// </summary>
    public EventSequenceMutationIdentity TargetSequence => Definition.Request.TargetSequence;

    /// <summary>
    /// Gets the originating event.
    /// </summary>
    public EventSequenceMutationOrigin Origin => Definition.Request.Origin;

    /// <summary>
    /// Gets the mutation kind.
    /// </summary>
    public EventSequenceMutationKind Kind => Definition.Request.Kind;

    /// <summary>
    /// Gets the mutation command envelope.
    /// </summary>
    public EventSequenceMutationCommandEnvelope Command => Definition.Request.Command;

    /// <summary>
    /// Gets the frozen target.
    /// </summary>
    public EventSequenceMutationTarget Target => Definition.Target;
}
