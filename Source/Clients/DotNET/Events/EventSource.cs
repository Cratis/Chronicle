// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents the identifier for an event source.
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventSource(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="EventSource"/>.
    /// </summary>
    public static readonly EventSource Unspecified = new(string.Empty);

    /// <summary>
    /// Gets the default <see cref="EventSource"/>.
    /// </summary>
    public static readonly EventSource Default = new("default");

    /// <summary>
    /// Convert from a <see cref="string"/> to an <see cref="EventSource"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EventSource(string value) => new(value);
}
