// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents configuration for Chronicle's internal identity provider.
/// </summary>
public class IdentityProviderOptions
{
    /// <summary>
    /// Gets or inits the legacy identity provider certificate configuration.
    /// This setting is accepted for compatibility but ignored. The top-level <see cref="ChronicleOptions.Tls"/>
    /// configuration serves all endpoints, including the internal identity provider.
    /// </summary>
    public Tls? Certificate { get; init; }
}