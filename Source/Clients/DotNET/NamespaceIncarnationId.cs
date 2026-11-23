// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>Identifies one kernel-issued incarnation of a namespace.</summary>
/// <param name="Value">The durable namespace identity.</param>
public record NamespaceIncarnationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>Converts a UUID to a namespace incarnation identity.</summary>
    /// <param name="value">The identity.</param>
    public static implicit operator NamespaceIncarnationId(Guid value) => new(value);
}
