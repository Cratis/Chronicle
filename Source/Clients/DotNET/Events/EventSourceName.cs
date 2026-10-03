// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents the name of a registered event source definition an event was appended through.
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventSourceName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an event that was not appended through a registered event source.
    /// </summary>
    public static readonly EventSourceName NotSet = new(string.Empty);

    /// <summary>
    /// Gets a value indicating whether the name is set.
    /// </summary>
    public bool IsSet => !string.IsNullOrEmpty(Value);

    /// <summary>
    /// Convert from a <see cref="string"/> to an <see cref="EventSourceName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EventSourceName(string value) => new(value);
}
