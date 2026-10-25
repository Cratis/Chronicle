// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Registrations;

/// <summary>
/// Represents a client-computed artifact fingerprint, opaque to the kernel.
/// </summary>
/// <param name="Value">The fingerprint.</param>
public record ArtifactFingerprint(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The value representing no fingerprint.
    /// </summary>
    public static readonly ArtifactFingerprint NotSet = string.Empty;

    /// <summary>
    /// Implicitly converts a string to a fingerprint.
    /// </summary>
    /// <param name="value">The fingerprint.</param>
    public static implicit operator ArtifactFingerprint(string value) => new(value);
}
