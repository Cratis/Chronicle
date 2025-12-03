// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the identifier for an event source type.
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventSourceType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="EventSourceType"/>.
    /// </summary>
    public static readonly EventSourceType Unspecified = new(string.Empty);

    /// <summary>
    /// Gets the default <see cref="EventSourceType"/>.
    /// </summary>
    public static readonly EventSourceType Default = new("Default");

    /// <summary>
    /// Gets a value indicating whether this <see cref="EventSourceType"/> is <see cref="Default"/> or <see cref="Unspecified"/>.
    /// </summary>
    public bool IsDefaultOrUnspecified => this == Default || this == Unspecified;

    /// <summary>
    /// Convert from a <see cref="string"/> to an <see cref="EventSourceType"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EventSourceType(string value) => new(value);
}
