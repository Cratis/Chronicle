// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents a client configuration for bootstrap.
/// The secret is provided as plaintext in config (expected from secrets management)
/// and is hashed internally on load.
/// </summary>
public class ClientBootstrapConfig
{
    /// <summary>
    /// Gets or inits the client identifier.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the client secret in plaintext. This value is hashed on load
    /// and never retained in memory beyond bootstrap.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;
}
