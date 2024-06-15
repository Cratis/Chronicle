// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Cratis.Chronicle.Net;

/// <summary>
/// Extension methods for working with <see cref="IServerAddressesFeature"/>.
/// </summary>
public static class UriExtensions
{
    /// <summary>
    /// Gets the first address as a <see cref="Uri"/>.
    /// </summary>
    /// <param name="addresses">The <see cref="IServerAddressesFeature"/> to get from.</param>
    /// <returns>The URI.</returns>'
    public static Uri GetFirstAddressAsUri(this IServerAddressesFeature addresses)
    {
        var address = addresses.Addresses.FirstOrDefault(a => a.StartsWith("http:"));
        address ??= addresses.Addresses.First();
        address = address.Replace("//+", "//localhost").Replace("//*", "//localhost");
        return new Uri(address);
    }

    /// <summary>
    /// Adjust a URI for Docker host. If running in a container and Uri contains localhost, it will replace localhost with 'host.docker.internal'.
    /// </summary>
    /// <param name="uri">Uri to get actual client uri from.</param>
    /// <returns>The actual uri.</returns>
    public static Uri AdjustForDockerHost(this Uri uri)
    {
        var address = uri.ToString();
        var container = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        if (bool.TryParse(container, out var isRunningInContainer)
            && isRunningInContainer
            && address.Contains("localhost"))
        {
            address = address.Replace("localhost", "host.docker.internal");
        }

        return new Uri(address);
    }
}
