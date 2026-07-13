// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the TLS configuration.
/// </summary>
public class Tls
{
    /// <summary>
    /// Gets or inits whether TLS is enabled. Defaults to <see langword="true"/>, serving gRPC and the Workbench, API and OAuth
    /// flows on a single multiplexed TLS <see cref="ChronicleOptions.Port"/>.
    /// Set to <see langword="false"/> to run in cleartext — for example when TLS is terminated upstream by an ingress or reverse
    /// proxy, or for local development — in which case gRPC (h2c) is served on <see cref="ChronicleOptions.Port"/> and
    /// the HTTP/1.1 surface on <see cref="ChronicleOptions.ManagementPort"/>. No certificate is required when disabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or inits the path to the certificate file for TLS.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Gets or inits the password for the certificate file.
    /// </summary>
    public string? CertificatePassword { get; init; }
}
