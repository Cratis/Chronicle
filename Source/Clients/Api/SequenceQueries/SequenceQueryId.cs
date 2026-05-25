// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Represents the unique identifier of a sequence query.
/// </summary>
/// <param name="Value">The underlying <see cref="Guid"/> value.</param>
public record SequenceQueryId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the unset value.
    /// </summary>
    public static readonly SequenceQueryId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from a <see cref="SequenceQueryId"/> to a <see cref="Guid"/>.
    /// </summary>
    /// <param name="id"><see cref="SequenceQueryId"/> to convert from.</param>
    public static implicit operator Guid(SequenceQueryId id) => id.Value;

    /// <summary>
    /// Implicitly convert from a <see cref="Guid"/> to a <see cref="SequenceQueryId"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> to convert from.</param>
    public static implicit operator SequenceQueryId(Guid value) => new(value);

    /// <summary>
    /// Creates a new <see cref="SequenceQueryId"/> with a new unique value.
    /// </summary>
    /// <returns>A new <see cref="SequenceQueryId"/>.</returns>
    public static SequenceQueryId New() => new(Guid.NewGuid());
}
