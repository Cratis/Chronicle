// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an application display name.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ApplicationDisplayName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="ApplicationDisplayName"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="ApplicationDisplayName"/>.</returns>
    public static implicit operator ApplicationDisplayName(string value) => new(value);
}
