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

    /// <summary>
    /// Applies the same trusted proxy policy to ASP.NET Core's automatically inserted middleware
    /// and to the middleware Chronicle inserts when automatic forwarding is disabled.
    /// </summary>
    /// <param name="services">The kernel services.</param>
    /// <param name="configuration">The Chronicle options.</param>
    internal static void Configure(IServiceCollection services, ChronicleOptions configuration)
    {
        var trust = Create(configuration);
        services.PostConfigure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = trust.ForwardedHeaders;
            options.KnownProxies.Clear();
            foreach (var proxy in trust.KnownProxies)
            {
                options.KnownProxies.Add(proxy);
            }

            options.KnownIPNetworks.Clear();
            foreach (var network in trust.KnownIPNetworks)
            {
                options.KnownIPNetworks.Add(network);
            }
        });
    }

    /// <summary>
    /// Installs Chronicle's forwarded-headers middleware only when ASP.NET Core has not installed its own.
    /// </summary>
    /// <param name="app">The kernel application.</param>
    /// <param name="configuration">The host configuration containing the framework's forwarding flag.</param>
    internal static void Use(WebApplication app, IConfiguration configuration)
    {
        if (!string.Equals(configuration["ForwardedHeaders_Enabled"], "true", StringComparison.OrdinalIgnoreCase))
        {
            app.UseForwardedHeaders();
        }
    }
}
