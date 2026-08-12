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
    /// Both values are normalized before comparison so that the configured endpoint may be written
    /// with or without a leading slash, and so that a probe hitting "/health/" reaches the same
    /// endpoint as one hitting "/health". The comparison ignores case because ASP.NET Core routing
    /// matches endpoint paths case-insensitively - matching more strictly here would reject requests
    /// the health endpoint itself would happily serve on the main port.
    /// </remarks>
    public static bool IsHealthCheckEndpoint(string? healthCheckEndpoint, string? path) =>
        string.Equals(Normalize(healthCheckEndpoint), Normalize(path), StringComparison.OrdinalIgnoreCase);

    static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "/";
        var trimmed = value.Trim().TrimEnd('/');
        if (trimmed.Length == 0) return "/";
        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }
}
