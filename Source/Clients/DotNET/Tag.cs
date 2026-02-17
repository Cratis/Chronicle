// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents a tag associated with an event, observer, or read model.
/// </summary>
/// <param name="Value">The value of the tag.</param>
public record Tag(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a string to a <see cref="Tag"/>.
    /// </summary>
    /// <param name="tag">The tag as a string.</param>
    public static implicit operator Tag(string tag) => new(tag);
}
