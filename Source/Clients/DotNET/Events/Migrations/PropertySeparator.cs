// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents the separator used when splitting a property value.
/// </summary>
/// <param name="Value">Actual value.</param>
public record PropertySeparator(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of a space separator.
    /// </summary>
    public static readonly PropertySeparator Space = new(" ");

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PropertySeparator"/>.
    /// </summary>
    /// <param name="separator"><see cref="string"/> to convert from.</param>
    public static implicit operator PropertySeparator(string separator) => new(separator);

    /// <summary>
    /// Implicitly convert from <see cref="PropertySeparator"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="separator"><see cref="PropertySeparator"/> to convert from.</param>
    public static implicit operator string(PropertySeparator separator) => separator.Value;
}
