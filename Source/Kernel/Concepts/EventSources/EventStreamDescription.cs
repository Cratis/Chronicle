// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSources;

/// <summary>
/// Represents the human-readable description of an event stream.
/// </summary>
/// <param name="Value">The actual value.</param>
public record EventStreamDescription(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of a description that is not set.
    /// </summary>
    public static readonly EventStreamDescription NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="EventStreamDescription"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EventStreamDescription(string value) => new(value);
}
