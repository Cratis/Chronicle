// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the database name of a database external service endpoint.
/// </summary>
/// <param name="Value">The actual value.</param>
public record DatabaseName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from a string to <see cref="DatabaseName"/>.
    /// </summary>
    /// <param name="name">String to convert from.</param>
    public static implicit operator DatabaseName(string name) => new(name);
}
