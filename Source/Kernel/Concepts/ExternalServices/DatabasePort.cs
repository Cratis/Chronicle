// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the port of a database external service endpoint.
/// </summary>
/// <param name="Value">The actual value.</param>
public record DatabasePort(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Represents an unspecified <see cref="DatabasePort"/>, meaning the provider default is used.
    /// </summary>
    public static readonly DatabasePort Unspecified = new(0);

    /// <summary>
    /// Implicitly convert from an int to <see cref="DatabasePort"/>.
    /// </summary>
    /// <param name="port">Int to convert from.</param>
    public static implicit operator DatabasePort(int port) => new(port);
}
