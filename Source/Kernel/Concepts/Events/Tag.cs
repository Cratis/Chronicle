// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents a tag that can be associated with events and observers.
/// </summary>
/// <param name="Value">The tag value.</param>
public record Tag(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty tag.
    /// </summary>
    public static readonly Tag Empty = new(string.Empty);

    /// <summary>
    /// Implicit conversion from <see cref="Tag"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static implicit operator string(Tag tag) => tag.Value;

    /// <summary>
    /// Implicit conversion from <see cref="string"/> to <see cref="Tag"/>.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    public static implicit operator Tag(string value) => new(value);
}
