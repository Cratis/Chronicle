// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Generates self-signed certificates for benchmark infrastructure.
/// </summary>
public static class BenchmarkCertificateGenerator
{
    /// <summary>
    /// Generates and saves a self-signed certificate to a PFX file.
    /// </summary>
    /// <param name="filePath">The path where the PFX file will be saved.</param>
    /// <param name="password">The password used to protect the PFX file.</param>
    /// <param name="subjectName">The certificate subject name.</param>
    /// <param name="validityDays">The number of days the certificate remains valid.</param>
    /// <returns>The generated certificate path.</returns>
    public static string GenerateAndSaveCertificate(
        string filePath,
        string password,
        string subjectName = "CN=localhost",
        int validityDays = 365)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var certificate = GenerateSelfSignedCertificate(subjectName, validityDays);
        var pfxBytes = certificate.Export(X509ContentType.Pfx, password);
        File.WriteAllBytes(filePath, pfxBytes);
        return filePath;
    }

    static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, int validityDays)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            subjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
        subjectAlternativeNames.AddDnsName("localhost");
        subjectAlternativeNames.AddDnsName("chronicle");
        subjectAlternativeNames.AddIpAddress(System.Net.IPAddress.Loopback);
        subjectAlternativeNames.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(subjectAlternativeNames.Build());

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [
                    new Oid("1.3.6.1.5.5.7.3.1"),
                    new Oid("1.3.6.1.5.5.7.3.2")
                ],
                critical: true));

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true));

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddDays(validityDays);

        return request.CreateSelfSigned(notBefore, notAfter);
    }
}
