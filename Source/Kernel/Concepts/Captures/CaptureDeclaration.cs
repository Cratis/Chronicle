// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the capture declaration language source text a capture is defined from.
/// </summary>
/// <param name="Value">Inner value.</param>
public record CaptureDeclaration(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents the "not set" <see cref="CaptureDeclaration"/>.
    /// </summary>
    public static readonly CaptureDeclaration NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="CaptureDeclaration"/>.
    /// </summary>
    /// <param name="declaration"><see cref="string"/> representation.</param>
    public static implicit operator CaptureDeclaration(string declaration) => new(declaration);
}
