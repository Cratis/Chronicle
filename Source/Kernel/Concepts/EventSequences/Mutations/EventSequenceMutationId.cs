// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Represents the unique identifier of an event sequence mutation.
/// </summary>
/// <param name="Value">The unique identifier.</param>
public record EventSequenceMutationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the identifier representing a value that is not set.
    /// </summary>
    public static readonly EventSequenceMutationId NotSet = Guid.Empty;

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to an <see cref="EventSequenceMutationId"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted mutation identifier.</returns>
    public static implicit operator EventSequenceMutationId(Guid value) => new(value);
}
