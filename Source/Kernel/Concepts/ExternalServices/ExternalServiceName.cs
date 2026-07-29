// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the human-readable name of an external service.
/// </summary>
/// <param name="Value">The actual value.</param>
public record ExternalServiceName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ExternalServiceName"/>.
    /// </summary>
    public static readonly ExternalServiceName Unspecified = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="ExternalServiceName"/>.
    /// </summary>
    /// <param name="name">String to convert from.</param>
    public static implicit operator ExternalServiceName(string name) => new(name);
}
