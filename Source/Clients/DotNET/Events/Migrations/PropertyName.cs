// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Represents the name of a property in an event type.
/// </summary>
/// <param name="Value">Actual value.</param>
public record PropertyName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of a not-set <see cref="PropertyName"/>.
    /// </summary>
    public static readonly PropertyName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="PropertyName"/>.
    /// </summary>
    /// <param name="name"><see cref="string"/> to convert from.</param>
    public static implicit operator PropertyName(string name) => new(name);

    /// <summary>
    /// Implicitly convert from <see cref="PropertyName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="name"><see cref="PropertyName"/> to convert from.</param>
    public static implicit operator string(PropertyName name) => name.Value;
}
