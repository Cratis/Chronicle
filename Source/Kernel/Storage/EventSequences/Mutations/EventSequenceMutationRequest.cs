// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents an immutable request to register an event sequence mutation.
/// </summary>
/// <param name="Id">The unique mutation identifier.</param>
/// <param name="TargetSequence">The validated identity of the sequence to mutate.</param>
/// <param name="Origin">The event that originated the mutation.</param>
/// <param name="Kind">The mutation kind.</param>
/// <param name="Command">The command envelope.</param>
public sealed record EventSequenceMutationRequest(
    EventSequenceMutationId Id,
    EventSequenceMutationIdentity TargetSequence,
    EventSequenceMutationOrigin Origin,
    EventSequenceMutationKind Kind,
    EventSequenceMutationCommandEnvelope Command)
{
    /// <summary>
    /// Determines whether another request is exactly the same request.
    /// </summary>
    /// <param name="other">The request to compare.</param>
    /// <returns><see langword="true"/> when every request field, including payload text ordinally, is equal.</returns>
    public bool ExactlyEquals(EventSequenceMutationRequest? other) =>
        Origin is { Sequence: not null, SequenceNumber: not null } &&
        Command is { Payload: not null, Hash: not null } &&
        other is
        {
            Origin: { Sequence: not null, SequenceNumber: not null },
            Command: { Payload: not null, Hash: not null }
        } &&
        Id == other.Id &&
        IdentityEquals(TargetSequence, other.TargetSequence) &&
        IdentityEquals(Origin.Sequence, other.Origin.Sequence) &&
        Origin.SequenceNumber == other.Origin.SequenceNumber &&
        Kind == other.Kind &&
        string.Equals(Command.Payload, other.Command.Payload, StringComparison.Ordinal) &&
        string.Equals(Command.Hash.Value, other.Command.Hash.Value, StringComparison.Ordinal);

    static bool IdentityEquals(EventSequenceMutationIdentity? left, EventSequenceMutationIdentity? right) =>
        left is not null &&
        right is not null &&
        string.Equals(left.Display, right.Display, StringComparison.Ordinal) &&
        left.Key == right.Key;
}
