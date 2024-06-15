// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Extension methods for working with <see cref="Uri"/>.
/// </summary>
public static class UriExtensionMethods
{
    /// <summary>
    /// Convert a HTTP/HTTPS endpoint to a WS/WSS based one.
    /// </summary>
    /// <param name="uri">Uri to convert.</param>
    /// <returns>A WS/WSS endpoint.</returns>
    public static Uri ToWebSocketEndpoint(this Uri uri)
    {
        var scheme = uri.Scheme == "http" ? "ws" : "wss";
        var endpointAsString = uri.ToString();
        endpointAsString = endpointAsString.Replace(uri.Scheme, scheme);
        return new Uri(endpointAsString);
    }
}
