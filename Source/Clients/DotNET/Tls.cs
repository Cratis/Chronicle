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
}
