// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Aksio.Cratis.Net;

/// <summary>
/// Extension methods for working with <see cref="IServerAddressesFeature"/>.
/// </summary>
public static class ServerAddressFeatureExtensions
{
    /// <summary>
    /// Gets the first address as a <see cref="Uri"/>.
    /// </summary>
    /// <param name="addresses">The <see cref="IServerAddressesFeature"/> to get from.</param>
    /// <returns>The URI.</returns>
    public static Uri GetFirstAddressAsUri(this IServerAddressesFeature addresses)
    {
        var address = addresses.Addresses.First();
        address = address.Replace("//+", "//localhost").Replace("//*", "//localhost");
        return new Uri(address);
    }
}
