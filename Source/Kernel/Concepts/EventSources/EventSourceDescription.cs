// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSources;

/// <summary>
/// Represents the human-readable description of an event source.
/// </summary>
/// <param name="Value">The actual value.</param>
public record EventSourceDescription(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of a description that is not set.
    /// </summary>
    public static readonly EventSourceDescription NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="EventSourceDescription"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EventSourceDescription(string value) => new(value);
}
