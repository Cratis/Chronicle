// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents an implementation of <see cref="IEncryption"/>.
/// </summary>
/// <param name="chronicleOptions"><see cref="IOptions{ChronicleOptions}"/> for getting Chronicle configuration.</param>
[Singleton]
public class Encryption(IOptions<ChronicleOptions> chronicleOptions) : IEncryption
{
#if DEVELOPMENT
    const string DefaultCertificateFolder = "certificates";
    const string DefaultCertificateFileName = "encryption-cert.pfx";
    const string DefaultCertificatePassword = "chronicle-auto-generated";
#endif

    readonly ChronicleOptions _options = chronicleOptions.Value;

    /// <inheritdoc/>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        using var certificate = LoadCertificate();
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is not null)
        {
            var dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
            var encryptedData = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedData);
        }

        throw new MissingPublicKeyInCertificate();
    }

    /// <inheritdoc/>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        using var certificate = LoadCertificate();
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa is not null)
        {
            var dataToDecrypt = Convert.FromBase64String(encryptedText);
            var decryptedData = rsa.Decrypt(dataToDecrypt, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decryptedData);
        }

        throw new MissingPrivateKeyInCertificate();
    }

    X509Certificate2 LoadCertificate()
    {
        var encryptionCert = _options.EncryptionCertificate;

        // If a certificate is configured and exists, use it
        if (encryptionCert.IsConfigured && File.Exists(encryptionCert.CertificatePath))
        {
#if NET8_0
            return new X509Certificate2(
                encryptionCert.CertificatePath,
                encryptionCert.CertificatePassword);
#else
            return X509CertificateLoader.LoadPkcs12FromFile(
                encryptionCert.CertificatePath,
                encryptionCert.CertificatePassword);
#endif
        }

#if DEVELOPMENT
        // For development: check for or generate a self-signed certificate
        return LoadOrGenerateDevelopmentCertificate();
#else
        // In production, a certificate is required
        throw new EncryptionCertificateNotConfigured();
#endif
    }

#if DEVELOPMENT
    X509Certificate2 LoadOrGenerateDevelopmentCertificate()
    {
        var certificateFolder = Path.Combine(Directory.GetCurrentDirectory(), DefaultCertificateFolder);
        var certificatePath = Path.Combine(certificateFolder, DefaultCertificateFileName);

        // If the certificate already exists, load it
        if (File.Exists(certificatePath))
        {
#if NET8_0
            return new X509Certificate2(certificatePath, DefaultCertificatePassword);
#else
            return X509CertificateLoader.LoadPkcs12FromFile(certificatePath, DefaultCertificatePassword);
#endif
        }

        // Generate a new self-signed certificate for development
        Directory.CreateDirectory(certificateFolder);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Chronicle Development Encryption",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add key usage extensions
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
                critical: true));

        // Create a self-signed certificate valid for 10 years
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(10));

        // Export to PFX with password
        var pfxData = certificate.Export(X509ContentType.Pfx, DefaultCertificatePassword);
        File.WriteAllBytes(certificatePath, pfxData);

#if NET8_0
        return new X509Certificate2(certificatePath, DefaultCertificatePassword);
#else
        return X509CertificateLoader.LoadPkcs12FromFile(certificatePath, DefaultCertificatePassword);
#endif
    }
#endif
}
