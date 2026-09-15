// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Represents the result of trying to create an <see cref="EventSequenceMutationIdentity"/>.
/// </summary>
public readonly record struct EventSequenceMutationIdentityCreationResult
{
    EventSequenceMutationIdentityCreationResult(EventSequenceMutationIdentity? identity, UnsupportedEventSequenceIdReason? reason)
    {
        Identity = identity;
        Reason = reason;
    }

    /// <summary>
    /// Gets whether identity creation succeeded.
    /// </summary>
    public bool IsSuccess => Identity is not null;

    /// <summary>
    /// Gets the created identity, or <see langword="null"/> when creation failed.
    /// </summary>
    public EventSequenceMutationIdentity? Identity { get; }

    /// <summary>
    /// Gets the typed failure reason, or <see langword="null"/> when creation succeeded.
    /// </summary>
    public UnsupportedEventSequenceIdReason? Reason { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="identity">The created identity.</param>
    /// <returns>A successful creation result.</returns>
    internal static EventSequenceMutationIdentityCreationResult Succeeded(EventSequenceMutationIdentity identity) => new(identity, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="reason">The typed failure reason.</param>
    /// <returns>A failed creation result.</returns>
    internal static EventSequenceMutationIdentityCreationResult Failed(UnsupportedEventSequenceIdReason reason) => new(null, reason);
}
