// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Identifies one physical lifetime of a namespace, independently of its reusable name.
/// </summary>
/// <param name="Value">The incarnation identity.</param>
public record NamespaceIncarnationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an unspecified incarnation. It is never a valid append precondition.
    /// </summary>
    public static readonly NamespaceIncarnationId NotSet = new(Guid.Empty);

    /// <summary>
    /// Converts an identity to its strongly typed representation.
    /// </summary>
    /// <param name="value">The identity.</param>
    public static implicit operator NamespaceIncarnationId(Guid value) => new(value);

    /// <summary>
    /// Creates a fresh incarnation identity.
    /// </summary>
    /// <returns>A new identity.</returns>
    public static NamespaceIncarnationId New() => new(Guid.NewGuid());
}
