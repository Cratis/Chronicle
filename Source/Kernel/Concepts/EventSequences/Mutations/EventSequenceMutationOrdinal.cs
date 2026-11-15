// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Represents the ordinal assigned to an event sequence mutation.
/// </summary>
/// <param name="Value">The ordinal value.</param>
public record EventSequenceMutationOrdinal(long Value) : ConceptAs<long>(Value)
{
    /// <summary>
    /// Gets the ordinal representing a value that is not set.
    /// </summary>
    public static readonly EventSequenceMutationOrdinal NotSet = 0L;

    /// <summary>
    /// Gets the first assignable mutation ordinal.
    /// </summary>
    public static readonly EventSequenceMutationOrdinal First = 1L;

    /// <summary>
    /// Implicitly converts a <see cref="long"/> to an <see cref="EventSequenceMutationOrdinal"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted mutation ordinal.</returns>
    public static implicit operator EventSequenceMutationOrdinal(long value) => new(value);
}
