// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents configuration for exposing the health endpoint on a dedicated port.
/// </summary>
/// <remarks>
/// The main Chronicle port multiplexes gRPC (HTTP/2) and HTTP/1.1 traffic over a single
/// TLS port and therefore always requires a certificate. The health endpoint is HTTP/1.1
/// only, so it can be published on its own dedicated port where TLS is optional. This is
/// useful for orchestrator and load-balancer probes that cannot validate the server's
/// certificate — for example a Kubernetes cluster where the main port serves a self-signed
/// certificate. When <see cref="Port"/> is not set, the health endpoint is served on the
/// main <see cref="ChronicleOptions.Port"/>.
/// </remarks>
public class Health
{
    /// <summary>
    /// Gets or inits the optional dedicated port for the health endpoint.
    /// </summary>
    /// <remarks>
    /// When not set, the health endpoint is served on the main multiplexed
    /// <see cref="ChronicleOptions.Port"/>. When set to a value other than the main port,
    /// the health endpoint is additionally served on this dedicated HTTP/1.1 port.
    /// </remarks>
    public int? Port { get; init; }

    /// <summary>
    /// Gets or inits whether the dedicated health <see cref="Port"/> uses TLS. Defaults to true.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="Port"/> is set. Set to false to serve the health endpoint
    /// in cleartext on the dedicated port for probes that cannot validate the server's
    /// certificate. The main <see cref="ChronicleOptions.Port"/> always uses TLS regardless of
    /// this setting.
    /// </remarks>
    public bool Tls { get; init; } = true;
}
