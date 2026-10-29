// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents the TLS configuration for Chronicle.
/// </summary>
public class Tls
{
    bool _skipCertificateValidation;

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
    /// Defaults to false. If neither this option nor the connection string specifies a value,
    /// loopback-only targets skip validation for local development. An explicit false validates even
    /// loopback certificates; true accepts any certificate. An explicit connection-string value wins.
    /// This option has no effect when TLS is disabled.
    /// </remarks>
    public bool SkipCertificateValidation
    {
        get => _skipCertificateValidation;
        set
        {
            _skipCertificateValidation = value;
            HasExplicitSkipCertificateValidation = true;
        }
    }

    /// <summary>
    /// Gets whether certificate validation was explicitly configured.
    /// </summary>
    internal bool HasExplicitSkipCertificateValidation { get; private set; }
}
