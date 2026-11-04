// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Represents the content digest of a projection definition, not a projection generation or replay identity.
/// </summary>
/// <param name="Value">The hexadecimal SHA-256 digest.</param>
public record ProjectionDefinitionDigest(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a string to a <see cref="ProjectionDefinitionDigest"/>.
    /// </summary>
    /// <param name="value">The digest.</param>
    public static implicit operator ProjectionDefinitionDigest(string value) => new(value);
}
