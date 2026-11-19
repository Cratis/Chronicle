// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.given;

/// <summary>
/// Creates the certificates the specs swap between.
/// </summary>
public static class TestCertificates
{
    /// <summary>
    /// The password of every exported keystore.
    /// </summary>
    public const string Password = "pw";

    /// <summary>
    /// Creates a self-signed certificate.
    /// </summary>
    /// <param name="subject">The subject, such as CN=first.</param>
    /// <returns>The certificate with its private key.</returns>
    public static X509Certificate2 Create(string subject)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pkcs12, Password), Password, X509KeyStorageFlags.Exportable);
    }

    /// <summary>
    /// Exports a certificate the way it is stored in a keystore file.
    /// </summary>
    /// <param name="certificate">The certificate to export.</param>
    /// <returns>The PKCS#12 bytes.</returns>
    public static byte[] Export(X509Certificate2 certificate) => certificate.Export(X509ContentType.Pkcs12, Password);

    /// <summary>
    /// Creates a certificate whose exported keystore has exactly the length of another one, which is what a renewal of the same shape produces.
    /// </summary>
    /// <param name="subject">The subject, which should have the same length as the subject of <paramref name="sameLengthAs"/>.</param>
    /// <param name="sameLengthAs">The certificate whose keystore length to match.</param>
    /// <returns>The certificate with its private key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no certificate of that length was produced.</exception>
    public static X509Certificate2 CreateWithSameLength(string subject, X509Certificate2 sameLengthAs)
    {
        var length = Export(sameLengthAs).Length;
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var candidate = Create(subject);
            if (Export(candidate).Length == length)
            {
                return candidate;
            }

            candidate.Dispose();
        }

        throw new InvalidOperationException("Could not create a certificate whose keystore has the same length.");
    }
}
