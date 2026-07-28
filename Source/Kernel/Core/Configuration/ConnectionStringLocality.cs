// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Provides locality checks over a storage connection string, used to guard against
/// forming an isolated cluster when localhost clustering runs against shared storage.
/// </summary>
public static class ConnectionStringLocality
{
    static readonly HashSet<string> _localHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1",
        "0.0.0.0"
    };

    /// <summary>
    /// Determines whether a storage connection string points at a host that is not local to the machine.
    /// </summary>
    /// <param name="connectionString">The storage connection string to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when at least one host in the connection string is non-local;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// A connection string with no discernible host - empty, or one that lists only loopback hosts -
    /// is treated as local so the split-cluster guard never warns on a genuine single-node local setup.
    /// </remarks>
    public static bool IsNonLocal(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return ExtractHosts(connectionString).Any(IsNonLocalHost);
    }

    static bool IsNonLocalHost(string host) =>
        !_localHosts.Contains(host) && !host.StartsWith("127.", StringComparison.Ordinal);

    static IEnumerable<string> ExtractHosts(string connectionString)
    {
        var authority = connectionString;

        var schemeIndex = authority.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex >= 0)
        {
            authority = authority[(schemeIndex + 3)..];
        }

        var credentialsIndex = authority.LastIndexOf('@');
        if (credentialsIndex >= 0)
        {
            authority = authority[(credentialsIndex + 1)..];
        }

        var pathIndex = authority.IndexOfAny(['/', '?']);
        if (pathIndex >= 0)
        {
            authority = authority[..pathIndex];
        }

        return authority
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(HostOf)
            .Where(host => host.Length > 0);
    }

    static string HostOf(string hostAndPort)
    {
        if (hostAndPort.StartsWith('['))
        {
            var end = hostAndPort.IndexOf(']');
            return end > 0 ? hostAndPort[1..end] : hostAndPort;
        }

        var portIndex = hostAndPort.IndexOf(':');
        return portIndex >= 0 ? hostAndPort[..portIndex] : hostAndPort;
    }
}
