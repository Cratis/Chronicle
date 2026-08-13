// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents the TLS configuration for Chronicle.
/// </summary>
public class Tls
{
    /// <summary>
    /// Gets or sets the path to the certificate file for TLS.
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the password for the certificate file.
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Gets or sets whether to skip TLS certificate validation when connecting.
    /// </summary>
    /// <remarks>
    /// The client always connects over TLS. This defaults to <see langword="true"/>: the server's
    /// certificate is not validated, and any certificate, including self-signed ones, is accepted.
    /// Set this to <see langword="false"/> to require full certificate chain validation instead —
    /// only do so against a server whose certificate is verifiable (not a self-signed development
    /// certificate).
    /// </remarks>
    public bool SkipCertificateValidation { get; set; } = true;
}
