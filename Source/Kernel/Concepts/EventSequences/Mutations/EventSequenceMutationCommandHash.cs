// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Represents the hash of an event sequence mutation command.
/// </summary>
/// <param name="Value">The hash value.</param>
public record EventSequenceMutationCommandHash(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the hash representing a value that is not set.
    /// </summary>
    public static readonly EventSequenceMutationCommandHash NotSet = string.Empty;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to an <see cref="EventSequenceMutationCommandHash"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted mutation command hash.</returns>
    public static implicit operator EventSequenceMutationCommandHash(string value) => new(value);
}
