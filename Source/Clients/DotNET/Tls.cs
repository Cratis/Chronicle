// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents the TLS configuration for Chronicle.
/// </summary>
public class Tls
{
    bool _skipCertificateValidation = true;

    /// <summary>
    /// Gets or sets the path to the certificate file for TLS.
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the password for the certificate file.
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Gets or sets the path to a file with the certificate authorities (CA) the server certificate must chain to.
    /// </summary>
    /// <remarks>
    /// The file holds one or more CA certificates in PEM or PKCS#7 format. When set, it takes precedence over the
    /// connection string's <c language="csharp">caCertificatePath</c>. The certificate authorities are used only when
    /// certificate validation is enabled; the server certificate is then accepted only when its chain builds to one of them.
    /// </remarks>
    public string? CaCertificatePath { get; set; }

    /// <summary>
    /// Gets or sets whether to skip TLS certificate validation when connecting.
    /// </summary>
    /// <remarks>
    /// Defaults to true in this release; the next major release will require validation by default.
    /// Set this to false explicitly to require certificate validation. An explicit false in either
    /// this option or the connection string requires validation. Use true only as a development
    /// opt-out. This option has no effect when TLS is disabled.
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
