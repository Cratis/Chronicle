// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences;

/// <summary>
/// A coarse per-event-sequence counter that every revision and redaction advances.
/// </summary>
/// <remarks>
/// The value works like a seqlock: it is odd while a history mutation is in flight and even when settled.
/// A decision read captures it with its boundary; a commit is refused when the current value differs.
/// </remarks>
/// <param name="Value">The counter value.</param>
public record EventSequenceMutationEpoch(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>
    /// The epoch of a sequence that has never had its history mutated.
    /// </summary>
    public static readonly EventSequenceMutationEpoch Initial = 0UL;

    /// <summary>
    /// Gets a value indicating whether a history mutation is in flight (or was interrupted).
    /// </summary>
    public bool IsMutating => Value % 2 == 1;

    /// <summary>
    /// Implicitly convert from <see cref="ulong"/> to <see cref="EventSequenceMutationEpoch"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator EventSequenceMutationEpoch(ulong value) => new(value);

    /// <summary>
    /// Gets the next odd epoch, strictly greater than this one, marking a mutation as started.
    /// </summary>
    /// <returns>The new epoch.</returns>
    public EventSequenceMutationEpoch Begin() => IsMutating ? new(Value + 2) : new(Value + 1);

    /// <summary>
    /// Gets the next even epoch, strictly greater than this one, marking a mutation as finished.
    /// </summary>
    /// <returns>The new epoch.</returns>
    public EventSequenceMutationEpoch End() => IsMutating ? new(Value + 1) : new(Value + 2);

    /// <summary>
    /// Gets the epoch with an interrupted mutation closed, or this one when already settled.
    /// </summary>
    /// <returns>The settled epoch.</returns>
    public EventSequenceMutationEpoch Settled() => IsMutating ? new(Value + 1) : this;
}
