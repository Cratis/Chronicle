// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents the unique identifier of a capture definition.
/// </summary>
/// <param name="Value">Inner value.</param>
public record CaptureId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents the "not set" <see cref="CaptureId"/>.
    /// </summary>
    public static readonly CaptureId NotSet = Guid.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="CaptureId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> representation.</param>
    public static implicit operator CaptureId(Guid id) => new(id);

    /// <summary>
    /// Creates a new <see cref="CaptureId"/>.
    /// </summary>
    /// <returns>A new <see cref="CaptureId"/>.</returns>
    public static CaptureId New() => new(Guid.NewGuid());
}
