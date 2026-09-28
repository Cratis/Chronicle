// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Identifies the reverse proxies whose forwarded scheme and client IP Chronicle may trust.
/// </summary>
public class ForwardedHeaders
{
    /// <summary>
    /// Gets or inits the IP addresses of trusted immediate reverse proxies.
    /// Loopback proxies remain trusted by default.
    /// </summary>
    public IEnumerable<string> KnownProxies { get; init; } = [];

    /// <summary>
    /// Gets or inits the CIDR ranges of trusted immediate reverse proxies.
    /// Loopback proxies remain trusted by default.
    /// </summary>
    public IEnumerable<string> KnownNetworks { get; init; } = [];
}
