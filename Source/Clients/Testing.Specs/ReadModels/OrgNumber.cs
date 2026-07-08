// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A string-backed organization number used as the event source id of a company stream and as the join key a
/// read model matches on.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record OrgNumber(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to an <see cref="OrgNumber"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator OrgNumber(string value) => new(value);
}
