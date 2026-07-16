// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A strongly-typed member identifier value, backed by a <see cref="Guid"/> concept. Used as a child key in a
/// membership roster whose child rows join a member's name in from a separate member stream.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record MemberId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="MemberId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    public static implicit operator MemberId(Guid value) => new(value);
}
