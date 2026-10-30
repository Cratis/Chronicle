// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents an operator's server-local administrator recovery request.
/// </summary>
public class AdministratorRecoveryOptions
{
    /// <summary>
    /// Gets the existing active user to grant administrator privilege.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unique, single-use audit nonce.
    /// </summary>
    public string Nonce { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether recovery is allowed even when an active administrator exists.
    /// </summary>
    public bool Force { get; init; }
}
