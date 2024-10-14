// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Concept that represents the unique identifier of an Reactor.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ReactorId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ReactorId"/>.
    /// </summary>
    public static readonly ReactorId Unspecified = new("[unspecified]");

    /// <summary>
    /// Implicitly convert from a string to <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator ReactorId(string id) => new(id);
}
