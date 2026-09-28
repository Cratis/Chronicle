// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents the name of a named event tag.
/// </summary>
/// <param name="Value">The exact name.</param>
public record TagName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the unset name. It cannot be used in a named tag.
    /// </summary>
    public static readonly TagName NotSet = new(string.Empty);

    /// <summary>
    /// Converts a tag name to a string.
    /// </summary>
    /// <param name="name">The tag name.</param>
    public static implicit operator string(TagName name) => name.Value;

    /// <summary>
    /// Converts a string to a tag name.
    /// </summary>
    /// <param name="value">The name.</param>
    public static implicit operator TagName(string value) => new(value);
}
