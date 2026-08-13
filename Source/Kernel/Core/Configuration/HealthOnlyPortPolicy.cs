// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Decides whether a request must be rejected because it arrived on a dedicated health port that is
/// configured to serve the health endpoint exclusively.
/// </summary>
/// <remarks>
/// This is a pure decision deliberately kept out of the server's request pipeline so it can be
/// specified in isolation. The caller supplies the local port the connection was accepted on -
/// never a client-supplied value such as a Host or X-Forwarded-* header, which a caller could spoof
/// to reach endpoints the operator meant to keep off the probe port.
/// </remarks>
public static class HealthOnlyPortPolicy
{
    /// <summary>
    /// Determines whether a request must be rejected.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> in effect.</param>
    /// <param name="localPort">The local port the connection was accepted on.</param>
    /// <param name="path">The request path.</param>
    /// <returns>True if the request must be rejected, false if it must be served.</returns>
    public static bool ShouldReject(ChronicleOptions options, int localPort, string? path)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Health.Exclusive) return false;
        if (options.DedicatedHealthPort is not { } healthPort) return false;
        if (localPort != healthPort) return false;

        return !IsHealthCheckEndpoint(options.HealthCheckEndpoint, path);
    }

    /// <summary>
    /// Determines whether a request path addresses the configured health check endpoint.
    /// </summary>
    /// <param name="healthCheckEndpoint">The configured health check endpoint.</param>
    /// <param name="path">The request path.</param>
    /// <returns>True if the path addresses the health check endpoint, false if not.</returns>
    /// <remarks>
    /// The configured endpoint and the request path are normalized <em>differently</em>, and deliberately so.
    /// This check only makes the port safe if it agrees with routing about which string a request addresses;
    /// anything this check forgives that routing does not is a hole, because the request is then admitted and
    /// falls through to whatever the fallback serves.
    /// <para>
    /// So the request path gets only the normalization routing itself performs: a trailing slash is ignored,
    /// and matching is case-insensitive. It is specifically <em>not</em> whitespace-trimmed - "/health%20"
    /// decodes to "/health " and is a different route to ASP.NET Core, so trimming it here would admit it onto
    /// the port while routing sent it to the fallback.
    /// </para>
    /// <para>
    /// The configured endpoint is operator-supplied rather than attacker-supplied, so it is trimmed and may be
    /// written with or without a leading slash. An endpoint that is missing or only whitespace configures no
    /// reachable path, so nothing matches it and the port rejects everything - the safe direction for a
    /// misconfiguration on a port whose whole purpose is to expose one thing.
    /// </para>
    /// </remarks>
    public static bool IsHealthCheckEndpoint(string? healthCheckEndpoint, string? path)
    {
        if (string.IsNullOrWhiteSpace(healthCheckEndpoint)) return false;

        var endpoint = healthCheckEndpoint.Trim().TrimEnd('/');
        if (!endpoint.StartsWith('/')) endpoint = $"/{endpoint}";

        var requested = (path ?? string.Empty).TrimEnd('/');
        if (requested.Length == 0) requested = "/";

        return string.Equals(endpoint, requested, StringComparison.OrdinalIgnoreCase);
    }
}
