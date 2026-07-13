// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Resolves the set of <see cref="ServerEndpoint"/> the Chronicle server should bind from the configured
/// <see cref="ChronicleOptions"/>.
/// </summary>
/// <remarks>
/// Kestrel can only multiplex HTTP/1.1 and HTTP/2 on a single port over TLS, where ALPN negotiates the protocol
/// per connection. With TLS enabled (the default) a single multiplexed TLS port therefore serves everything.
/// With TLS disabled the two protocols must split across two cleartext ports — h2c gRPC on <see cref="ChronicleOptions.Port"/>
/// and plain HTTP/1.1 (Workbench, API, OAuth and health) on <see cref="ChronicleOptions.ManagementPort"/> — mirroring the
/// pre-16.0 topology.
/// </remarks>
public static class ServerEndpointResolver
{
    /// <summary>
    /// Resolves the endpoints to bind for the given options.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> to resolve endpoints from.</param>
    /// <returns>The ordered set of <see cref="ServerEndpoint"/> to bind.</returns>
    public static IReadOnlyList<ServerEndpoint> Resolve(ChronicleOptions options)
    {
        var endpoints = new List<ServerEndpoint>();

        if (options.Tls.Enabled)
        {
            // Single multiplexed TLS port: gRPC (HTTP/2) and the Workbench, API and OAuth flows (HTTP/1.1),
            // negotiated per connection through ALPN.
            endpoints.Add(new ServerEndpoint(options.Port, EndpointProtocols.Http1AndHttp2, UseTls: true));
        }
        else
        {
            // Cleartext cannot multiplex on a single port, so split the protocols across two ports:
            // h2c gRPC on the main port and plain HTTP/1.1 on the management port.
            endpoints.Add(new ServerEndpoint(options.Port, EndpointProtocols.Http2, UseTls: false));
            endpoints.Add(new ServerEndpoint(options.ManagementPort, EndpointProtocols.Http1, UseTls: false));
        }

        if (options.HealthPort > 0 && endpoints.TrueForAll(endpoint => endpoint.Port != options.HealthPort))
        {
            // Optional dedicated plaintext health-probe port, independent of TLS on the main port.
            endpoints.Add(new ServerEndpoint(options.HealthPort, EndpointProtocols.Http1, UseTls: false));
        }

        return endpoints;
    }
}
