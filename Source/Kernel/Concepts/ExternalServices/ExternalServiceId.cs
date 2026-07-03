// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the unique identifier of an external service.
/// </summary>
/// <param name="Value">The actual value.</param>
public record ExternalServiceId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ExternalServiceId"/>.
    /// </summary>
    public static readonly ExternalServiceId Unspecified = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="ExternalServiceId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator ExternalServiceId(string id) => new(id);
}
