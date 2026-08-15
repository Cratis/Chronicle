// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.for_EncryptionCertificateRotationDiagnostics.given;

public class a_data_protection_key_ring : Specification
{
    protected X509Certificate2 _activeCertificate;
    protected X509Certificate2 _previousCertificate;
    protected X509Certificate2 _retiredCertificate;
    protected IXmlRepository _dataProtectionKeys;

    readonly List<X509Certificate2> _certificates = [];

    void Establish()
    {
        _activeCertificate = CreateCertificate("chronicle-specs-active");
        _previousCertificate = CreateCertificate("chronicle-specs-previous");
        _retiredCertificate = CreateCertificate("chronicle-specs-retired");
        _dataProtectionKeys = Substitute.For<IXmlRepository>();
    }

    void Destroy() => _certificates.ForEach(_ => _.Dispose());

    protected void KeyRingHolds(params XElement[] keys) =>
        _dataProtectionKeys.GetAllElements().Returns(keys.ToList().AsReadOnly());

    protected EncryptionCertificateRotationDiagnostics DiagnosticsFor(params X509Certificate2[] ring) =>
        new(EncryptionCertificateRing.For(ring), _dataProtectionKeys);

    /// <summary>
    /// Builds a stored key the way Data Protection stores one: the descriptor's secret encrypted to a
    /// certificate by the very encryptor the kernel configures, so what is scanned is what is written.
    /// </summary>
    /// <param name="certificate">The certificate to encrypt the key to.</param>
    /// <returns>The stored key element.</returns>
    protected static XElement KeyProtectedBy(X509Certificate2 certificate)
    {
        var encryptor = new CertificateXmlEncryptor(certificate, NullLoggerFactory.Instance);
        var encrypted = encryptor.Encrypt(new XElement("masterKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))));

        return new XElement(
            "key",
            new XAttribute("id", Guid.NewGuid().ToString()),
            new XElement(
                "descriptor",
                new XElement("encryptedSecret", encrypted.EncryptedElement)));
    }

    /// <summary>
    /// Builds a stored key in the shape Data Protection writes when no certificate is configured — the
    /// secret is simply there, in the clear.
    /// </summary>
    /// <returns>The stored key element.</returns>
    protected static XElement KeyProtectedByNothing() =>
        new(
            "key",
            new XAttribute("id", Guid.NewGuid().ToString()),
            new XElement(
                "descriptor",
                new XElement("masterKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)))));

    X509Certificate2 CreateCertificate(string name)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        _certificates.Add(certificate);

        return certificate;
    }
}
