// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Represents the version of the persisted state for an event sequence mutation.
/// </summary>
/// <param name="Value">The state version value.</param>
public record EventSequenceMutationStateVersion(long Value) : ConceptAs<long>(Value)
{
    /// <summary>
    /// Gets the state version representing a value that is not set.
    /// </summary>
    public static readonly EventSequenceMutationStateVersion NotSet = 0L;

    /// <summary>
    /// Gets the first assignable mutation state version.
    /// </summary>
    public static readonly EventSequenceMutationStateVersion First = 1L;

    /// <summary>
    /// Implicitly converts a <see cref="long"/> to an <see cref="EventSequenceMutationStateVersion"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted mutation state version.</returns>
    public static implicit operator EventSequenceMutationStateVersion(long value) => new(value);

    /// <summary>
    /// Gets the next mutation state version.
    /// </summary>
    /// <returns>The next mutation state version.</returns>
    /// <exception cref="EventSequenceMutationStateVersionExhausted">Thrown when this version is <see cref="long.MaxValue"/>.</exception>
    public EventSequenceMutationStateVersion Next()
    {
        if (Value == long.MaxValue)
        {
            throw new EventSequenceMutationStateVersionExhausted(this);
        }

        return new(checked(Value + 1));
    }
}
