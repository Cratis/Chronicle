// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the name of a capture.
/// </summary>
/// <param name="Value">Inner value.</param>
public record CaptureName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents the "not set" <see cref="CaptureName"/>.
    /// </summary>
    public static readonly CaptureName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="CaptureName"/>.
    /// </summary>
    /// <param name="name"><see cref="string"/> representation.</param>
    public static implicit operator CaptureName(string name) => new(name);
}
