// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the opt-in claim that grants administrator privilege to callers authenticated by an external authority.
/// </summary>
public class ExternalAdministratorClaim
{
    /// <summary>
    /// Gets the exact, case-sensitive claim type, for example "roles".
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the exact, case-sensitive claim value, for example "chronicle-admin".
    /// </summary>
    public string? Value { get; init; }
}
