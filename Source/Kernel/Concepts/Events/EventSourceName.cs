// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the name of a registered event source definition.
/// </summary>
/// <param name="Value">The actual value.</param>
/// <remarks>
/// The name equals the <see cref="EventSourceType"/> that is written on events appended through the definition.
/// </remarks>
public record EventSourceName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an event source name that is not set, meaning the event was not appended through a registered definition.
    /// </summary>
    public static readonly EventSourceName NotSet = new(string.Empty);

    /// <summary>
    /// Gets a value indicating whether the name is set.
    /// </summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>
    /// Implicitly convert from a string to <see cref="EventSourceName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EventSourceName(string value) => new(value);
}
