// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Provides ephemeral development certificates (CA + server cert) for local development.
/// Included only when the <c>DEVELOPMENT</c> symbol is defined.
/// </summary>
internal static class DevCertificateProvider
{
    static X509Certificate2? _serverCertificate;
    static string? _caPem;

    /// <summary>
    /// Ensures a development CA and server certificate exist and returns them.
    /// </summary>
    /// <param name="subjectName">The server subject name (defaults to <c>localhost</c>).</param>
    /// <returns>Tuple of server certificate and CA PEM string.</returns>
    public static (X509Certificate2? ServerCertificate, string? CaPem) EnsureDevCertificate(string? subjectName = "localhost")
    {
        if (_serverCertificate is not null && _caPem is not null)
        {
            return (_serverCertificate, _caPem);
        }

        using var rsa = RSA.Create(2048);

        var caReq = new CertificateRequest("CN=Chronicle-Dev-CA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(caReq.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddYears(5);

        var caCert = caReq.CreateSelfSigned(notBefore, notAfter);

        // Create server cert signed by CA
        using var serverRsa = RSA.Create(2048);
        var serverReq = new CertificateRequest($"CN={subjectName}", serverRsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        serverReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        serverReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(serverReq.PublicKey, false));
        serverReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        serverReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false)); // serverAuth

        var serial = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
        var serverCert = serverReq.Create(caCert, notBefore, notAfter, serial);

        // Combine server cert with private key and keep the instance
        _serverCertificate = serverCert.CopyWithPrivateKey(serverRsa);

        // Export CA as PEM with inserted line breaks
        var caBase64WithBreaks = Convert.ToBase64String(caCert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks);
        _caPem = "-----BEGIN CERTIFICATE-----\n" + caBase64WithBreaks + "\n-----END CERTIFICATE-----\n";

        return (_serverCertificate, _caPem);
    }
}
#endif
