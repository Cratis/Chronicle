// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Generates self-signed certificates for testing purposes.
/// </summary>
public static class TestCertificateGenerator
{
    /// <summary>
    /// Generates a self-signed certificate for testing.
    /// </summary>
    /// <param name="subjectName">The subject name for the certificate (e.g., "CN=localhost").</param>
    /// <param name="validityDays">Number of days the certificate is valid. Defaults to 365.</param>
    /// <returns>A self-signed X509Certificate2 with private key.</returns>
    public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName = "CN=localhost", int validityDays = 365)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            subjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddDnsName("chronicle");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

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

    /// <summary>
    /// Saves a certificate to a PFX file.
    /// </summary>
    /// <param name="certificate">The certificate to save.</param>
    /// <param name="filePath">The path where the PFX file will be saved.</param>
    /// <param name="password">The password to protect the PFX file.</param>
    public static void SaveToPfxFile(X509Certificate2 certificate, string filePath, string password)
    {
        var pfxBytes = certificate.Export(X509ContentType.Pfx, password);
        File.WriteAllBytes(filePath, pfxBytes);
    }

    /// <summary>
    /// Generates and saves a self-signed certificate to a PFX file for testing.
    /// </summary>
    /// <param name="filePath">The path where the PFX file will be saved.</param>
    /// <param name="password">The password to protect the PFX file.</param>
    /// <param name="subjectName">The subject name for the certificate (e.g., "CN=localhost").</param>
    /// <param name="validityDays">Number of days the certificate is valid. Defaults to 365.</param>
    /// <returns>The path to the generated certificate file.</returns>
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
        SaveToPfxFile(certificate, filePath, password);
        return filePath;
    }
}
