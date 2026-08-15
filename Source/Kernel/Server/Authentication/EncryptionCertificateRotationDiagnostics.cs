// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Cratis.Chronicle.Security;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionCertificateRotationDiagnostics"/>.
/// </summary>
/// <param name="ring">The <see cref="IEncryptionCertificateRing"/> this node is running with.</param>
/// <param name="dataProtectionKeys">The <see cref="IXmlRepository"/> the Data Protection key ring is stored in.</param>
/// <remarks>
/// Data Protection encrypts each stored key to a certificate and records that certificate's full DER inside
/// the stored XML, so which certificate a key needs is read rather than guessed.
/// </remarks>
[Singleton]
public class EncryptionCertificateRotationDiagnostics(
    IEncryptionCertificateRing ring,
    IXmlRepository dataProtectionKeys) : IEncryptionCertificateRotationDiagnostics
{
    const string CertificateElementName = "X509Certificate";

    /// <inheritdoc/>
    public EncryptionCertificateRotationReport GetReport()
    {
        var attributions = dataProtectionKeys.GetAllElements()
            .Select(KeyIdFor)
            .ToArray();

        var dependencies = attributions
            .Where(_ => _ is not null)
            .GroupBy(_ => _!, StringComparer.OrdinalIgnoreCase)
            .Select(_ => new DataProtectionKeyDependency(_.Key, RoleFor(_.Key), _.Count()))
            .ToArray();

        return new(
            ring.GetStatus(),
            dependencies,
            attributions.Count(_ => _ is null));
    }

    static string? KeyIdFor(XElement key)
    {
        var certificate = key.Descendants()
            .FirstOrDefault(_ => string.Equals(_.Name.LocalName, CertificateElementName, StringComparison.Ordinal));

        if (certificate is null)
        {
            return null;
        }

        try
        {
            using var loaded = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificate.Value));
            return loaded.Thumbprint;
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            // The element is there but does not hold a certificate this runtime can read. Reporting it as
            // unattributed is the honest answer - claiming a thumbprint would be worse than admitting none.
            return null;
        }
    }

    EncryptionCertificateRole RoleFor(string keyId) =>
        ring.Find(keyId)?.Role ?? EncryptionCertificateRole.Retired;
}
