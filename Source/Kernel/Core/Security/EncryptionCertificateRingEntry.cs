// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents one certificate in the encryption-certificate ring.
/// </summary>
/// <param name="KeyId">The certificate thumbprint, which is how protected values identify the key they need.</param>
/// <param name="Role">The position the certificate holds in the ring.</param>
/// <param name="CertificatePath">The path the certificate was loaded from.</param>
/// <param name="Certificate">The loaded certificate.</param>
/// <remarks>
/// The ring owns the lifetime of <paramref name="Certificate"/> for as long as the process runs — Data
/// Protection and OpenIddict hold on to the same instances — so an entry never disposes it.
/// </remarks>
public record EncryptionCertificateRingEntry(
    string KeyId,
    EncryptionCertificateRole Role,
    string CertificatePath,
    X509Certificate2 Certificate)
{
    /// <summary>
    /// Describes this entry without exposing the certificate itself.
    /// </summary>
    /// <returns>The <see cref="EncryptionCertificateStatus"/> for this entry.</returns>
    public EncryptionCertificateStatus ToStatus() =>
        new(
            KeyId,
            Role,
            Certificate.Subject,
            new DateTimeOffset(Certificate.NotBefore.ToUniversalTime(), TimeSpan.Zero),
            new DateTimeOffset(Certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero),
            CertificatePath);
}
