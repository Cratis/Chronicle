// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Configuration;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring the cluster endpoint.
/// </summary>
public static class ClusterEndpointSiloBuilderExtensions
{
    /// <summary>
    /// Configure the cluster endpoint.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder ConfigureClusterEndpoint(this ISiloBuilder builder, ChronicleOptions options)
    {
        if (!string.IsNullOrEmpty(options.AdvertisedHostname))
        {
            builder.ConfigureEndpoints(
                hostname: options.AdvertisedHostname,
                siloPort: options.SiloPort,
                gatewayPort: options.GatewayPort,
                listenOnAnyHostAddress: true);
        }
        else
        {
            var address = string.IsNullOrEmpty(options.AdvertisedIPAddress)
                ? IPAddress.Loopback
                : IPAddress.Parse(options.AdvertisedIPAddress);

            builder.ConfigureEndpoints(
                advertisedIP: address,
                siloPort: options.SiloPort,
                gatewayPort: options.GatewayPort,
                listenOnAnyHostAddress: true);
        }

        return builder;
    }
}
