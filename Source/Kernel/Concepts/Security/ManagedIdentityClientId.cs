// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents the optional client identifier of a managed or workload identity.
/// </summary>
/// <param name="Value">The client identifier, or empty for the system-assigned identity.</param>
public record ManagedIdentityClientId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of a system-assigned identity.
    /// </summary>
    public static readonly ManagedIdentityClientId NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a string to a client identifier.
    /// </summary>
    /// <param name="value">The client identifier.</param>
    public static implicit operator ManagedIdentityClientId(string value) => new(value);
}
