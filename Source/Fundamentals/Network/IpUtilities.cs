// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.NetworkInformation;

namespace Client;

/// <summary>
/// Represents helper methods for working with the IP stack.
/// </summary>
/// <remarks>
/// Based on https://gist.github.com/jrusbatch/4211535?permalink_comment_id=3743591#gistcomment-3743591.
/// </remarks>
public static class IpUtilities
{
    const ushort MIN_PORT = 1;
    const ushort MAX_PORT = UInt16.MaxValue;

    /// <summary>
    /// Get first available port in a given port range.
    /// </summary>
    /// <param name="start">Start of the range. Defaults to 1.</param>
    /// <param name="end">End of the range.</param>
    /// <returns>An available port or null if no available. Defaults to 65535.</returns>
    public static int? GetAvailablePort(ushort start = MIN_PORT, ushort end = MAX_PORT)
    {
        var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        var usedPorts = Enumerable.Empty<int>()
            .Concat(ipProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint.Port))
            .Concat(ipProperties.GetActiveTcpListeners().Select(l => l.Port))
            .Concat(ipProperties.GetActiveUdpListeners().Select(l => l.Port))
            .ToHashSet();
        for (int port = start; port <= end; port++)
        {
            if (!usedPorts.Contains(port)) return port;
        }
        return null;
    }
}
