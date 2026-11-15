// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents something a principal may be permitted to do in the kernel.
/// </summary>
/// <param name="Value">The stable, machine-readable capability name.</param>
public record Capability(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="Capability"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="Capability"/>.</returns>
    public static implicit operator Capability(string value) => new(value);
}
