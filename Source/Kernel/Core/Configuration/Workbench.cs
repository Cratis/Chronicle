// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the Workbench configuration.
/// </summary>
public class Workbench
{
    /// <summary>
    /// Gets or inits the optional TLS configuration for the Workbench.
    /// When not set, falls back to the top-level <see cref="ChronicleOptions.Tls"/> configuration.
    /// </summary>
    public Tls? Tls { get; init; }
}
