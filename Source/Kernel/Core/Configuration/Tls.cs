// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the TLS configuration.
/// </summary>
public class Tls
{
    /// <summary>
    /// Gets or inits whether TLS is enabled. Defaults to true.
    /// Set to false only behind an HTTPS-terminating reverse proxy that forwards all Chronicle
    /// traffic using cleartext HTTP/2 (h2c). With authentication enabled, an external HTTPS
    /// authority must also be configured; Chronicle's internal token endpoint is unavailable.
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
