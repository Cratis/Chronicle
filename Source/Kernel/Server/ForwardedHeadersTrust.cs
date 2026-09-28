// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Configures trusted reverse proxies without removing ASP.NET Core's default loopback trust.
/// </summary>
internal static class ForwardedHeadersTrust
{
    /// <summary>
    /// Builds the forwarded-headers policy for the configured immediate proxies.
    /// </summary>
    /// <param name="configuration">The Chronicle options.</param>
    /// <returns>A policy that trusts only loopback and explicitly configured peers.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a proxy address or network is invalid.</exception>
    internal static ForwardedHeadersOptions Create(ChronicleOptions configuration)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
        };

        foreach (var proxy in configuration.ForwardedHeaders.KnownProxies)
        {
            if (!IPAddress.TryParse(proxy, out var address))
            {
                throw new InvalidOperationException($"ForwardedHeaders:KnownProxies must contain IP addresses. Invalid value: '{proxy}'.");
            }

            options.KnownProxies.Add(address);
        }

        foreach (var network in configuration.ForwardedHeaders.KnownNetworks)
        {
            if (!System.Net.IPNetwork.TryParse(network, out var parsedNetwork) || !network.Contains('/', StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"ForwardedHeaders:KnownNetworks must contain CIDR ranges. Invalid value: '{network}'.");
            }

            options.KnownIPNetworks.Add(parsedNetwork);
        }

        return options;
    }
}
