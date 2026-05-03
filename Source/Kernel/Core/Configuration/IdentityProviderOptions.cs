// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents configuration for Chronicle's internal identity provider.
/// </summary>
public class IdentityProviderOptions
{
    /// <summary>
    /// Gets or inits the optional certificate configuration used by the internal identity provider.
    /// When not set, Chronicle falls back to the top-level <see cref="ChronicleOptions.Tls"/> configuration.
    /// </summary>
    public Tls? Certificate { get; init; }
}