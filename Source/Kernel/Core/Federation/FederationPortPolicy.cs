// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Decides whether a request must be rejected because it arrived on the federation port but does not address the federation service.
/// </summary>
/// <remarks>
/// The federation listener is an additional listener on the same application, so without this every endpoint, the
/// Workbench, the API and the client gRPC services among them, would be reachable on it. The caller supplies the local
/// port the connection was accepted on, never a client-supplied value such as a Host header.
/// </remarks>
public static class FederationPortPolicy
{
    /// <summary>
    /// The path prefix of the federation service.
    /// </summary>
    public const string ServicePathPrefix = "/Cratis.Chronicle.Contracts.Federation.FederationPull/";

    /// <summary>
    /// The route Arc exposes the pull query on over HTTP. It is never served on any port.
    /// </summary>
    public const string HttpRoute = "/api/federation/pull";

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

        // Routing matches paths case-insensitively, so this must too, or a differently-cased path would slip past.
        var servesPull = path?.StartsWith(ServicePathPrefix, StringComparison.OrdinalIgnoreCase) == true;
        var isPull = servesPull || IsHttpRoute(path);
        var onFederationPort = IsFederationPort(options, localPort);

        // The pull service is anonymous with respect to OAuth because a peer authenticates with its client certificate,
        // so it may only ever be reached through the federation listener, and never over the HTTP route.
        if (isPull)
        {
            return !(servesPull && onFederationPort);
        }

        return onFederationPort;
    }

    /// <summary>
    /// Determines whether a connection was accepted by the federation listener, where a client certificate is mandatory.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> in effect.</param>
    /// <param name="localPort">The local port the connection was accepted on.</param>
    /// <returns>True when the connection is on the federation listener.</returns>
    public static bool IsFederationPort(ChronicleOptions options, int localPort)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Federation.Enabled && localPort == options.Federation.Port;
    }

    static bool IsHttpRoute(string? path) =>
        path?.TrimEnd('/').Equals(HttpRoute, StringComparison.OrdinalIgnoreCase) == true;
}
