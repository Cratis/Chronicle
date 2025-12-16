// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the TLS configuration.
/// </summary>
public class Tls
{
    /// <summary>
    /// Gets or inits the path to the certificate file for TLS.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Gets or inits the password for the certificate file.
    /// </summary>
    public string? CertificatePassword { get; init; }

    /// <summary>
    /// Gets or inits whether TLS is disabled. Default is false (TLS enabled).
    /// </summary>
    public bool Disable { get; init; }

    /// <summary>
    /// Port used by the development well-known certificate provisioning endpoint.
    /// Only used in development mode when the server generates a dev CA and exposes the public cert.
    /// </summary>
    public int WellKnownCertPort { get; init; } = 35001;
}
