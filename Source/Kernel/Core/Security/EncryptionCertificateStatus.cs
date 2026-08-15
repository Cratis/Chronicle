// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents what an operator can be told about one certificate in the ring, without exposing key material.
/// </summary>
/// <param name="KeyId">The certificate thumbprint, which is how protected values identify the key they need.</param>
/// <param name="Role">The position the certificate holds in the ring.</param>
/// <param name="Subject">The certificate subject.</param>
/// <param name="NotBefore">The point from which the certificate is valid.</param>
/// <param name="NotAfter">The point after which the certificate is no longer valid.</param>
/// <param name="CertificatePath">The path the certificate was loaded from.</param>
public record EncryptionCertificateStatus(
    string KeyId,
    EncryptionCertificateRole Role,
    string Subject,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string CertificatePath)
{
    /// <summary>
    /// Gets a value indicating whether the certificate's validity window has passed.
    /// </summary>
    /// <remarks>
    /// An expired certificate still decrypts — RSA does not consult the validity window — so a previous
    /// certificate that has expired keeps doing its job. It cannot be promoted back to active.
    /// </remarks>
    public bool HasExpired => DateTimeOffset.UtcNow > NotAfter;
}
