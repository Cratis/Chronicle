// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents an implementation of <see cref="IEncryption"/>.
/// </summary>
/// <param name="ring">The <see cref="IEncryptionCertificateRing"/> holding the active and previous certificates.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> to report a dependency on a previous certificate to.</param>
[Singleton]
public class Encryption(IEncryptionCertificateRing ring, ILogger<Encryption> logger) : IEncryption
{
    /// <summary>
    /// The marker that introduces a value carrying the key id of the certificate that protected it.
    /// </summary>
    /// <remarks>
    /// A value written before Chronicle labeled its ciphertext is bare base64 and carries no marker, so a
    /// value that does not start with this is read by trying every certificate in the ring.
    /// </remarks>
    public const string KeyIdPrefix = "crk1";

    const char KeyIdSeparator = ':';

#if DEVELOPMENT
    const string DefaultCertificateFolder = "certificates";
    const string DefaultCertificateFileName = "encryption-cert.pfx";
    const string DefaultCertificatePassword = "chronicle-auto-generated";
#endif

    readonly Lazy<IEncryptionCertificateRing> _ring = new(() => ResolveRing(ring));
    readonly ConcurrentDictionary<string, bool> _reportedPreviousKeyIds = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        var active = _ring.Value.Active;
        using var rsa = active.Certificate.GetRSAPublicKey() ?? throw new MissingPublicKeyInCertificate();
        var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256);

        return $"{KeyIdPrefix}{KeyIdSeparator}{active.KeyId}{KeyIdSeparator}{Convert.ToBase64String(encrypted)}";
    }

    /// <inheritdoc/>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        var resolved = _ring.Value;

        if (TryReadKeyId(encryptedText, out var keyId, out var cipherText))
        {
            var entry = resolved.Find(keyId) ?? throw new EncryptionCertificateNotInRing(keyId, resolved.All.Select(_ => _.KeyId));
            ReportDependencyOnPreviousCertificate(entry);

            return DecryptWith(entry.Certificate, cipherText);
        }

        // Written before ciphertext carried a key id, so the only way to find the certificate is to try each
        // in ring order. Select is deferred, so this stops at the first one that opens it.
        var match = resolved.All
            .Select(entry => (Entry: entry, PlainText: TryDecryptWith(entry.Certificate, encryptedText)))
            .FirstOrDefault(_ => _.PlainText is not null);

        if (match.PlainText is null)
        {
            throw new ValueNotDecryptableWithAnyCertificate(resolved.All.Select(_ => _.KeyId));
        }

        ReportDependencyOnPreviousCertificate(match.Entry);

        return match.PlainText;
    }

    static bool TryReadKeyId(string value, out string keyId, out string cipherText)
    {
        keyId = string.Empty;
        cipherText = string.Empty;

        if (!value.StartsWith($"{KeyIdPrefix}{KeyIdSeparator}", StringComparison.Ordinal))
        {
            return false;
        }

        var parts = value.Split(KeyIdSeparator, 3);
        if (parts.Length != 3)
        {
            return false;
        }

        keyId = parts[1];
        cipherText = parts[2];

        return true;
    }

    static string DecryptWith(X509Certificate2 certificate, string cipherText)
    {
        using var rsa = certificate.GetRSAPrivateKey() ?? throw new MissingPrivateKeyInCertificate();
        var decrypted = rsa.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.OaepSHA256);

        return Encoding.UTF8.GetString(decrypted);
    }

    static string? TryDecryptWith(X509Certificate2 certificate, string cipherText)
    {
        if (!certificate.HasPrivateKey)
        {
            return null;
        }

        try
        {
            return DecryptWith(certificate, cipherText);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            // This certificate is not the one that protected the value - OAEP padding does not verify, or the
            // value is not base64 at all. Neither is an error here; the caller decides what an exhausted ring means.
            return null;
        }
    }

    static IEncryptionCertificateRing ResolveRing(IEncryptionCertificateRing ring)
    {
        if (ring.IsConfigured)
        {
            return ring;
        }

#if DEVELOPMENT
        return EncryptionCertificateRing.For(LoadOrGenerateDevelopmentCertificate());
#else
        throw new EncryptionCertificateNotConfigured();
#endif
    }

#if DEVELOPMENT
    static X509Certificate2 LoadOrGenerateDevelopmentCertificate()
    {
        var certificateFolder = Path.Combine(Directory.GetCurrentDirectory(), DefaultCertificateFolder);
        var certificatePath = Path.Combine(certificateFolder, DefaultCertificateFileName);

        if (File.Exists(certificatePath))
        {
            return X509CertificateLoader.LoadPkcs12FromFile(certificatePath, DefaultCertificatePassword);
        }

        Directory.CreateDirectory(certificateFolder);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Chronicle Development Encryption",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
                critical: true));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(10));

        File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx, DefaultCertificatePassword));

        return X509CertificateLoader.LoadPkcs12FromFile(certificatePath, DefaultCertificatePassword);
    }
#endif

    void ReportDependencyOnPreviousCertificate(EncryptionCertificateRingEntry entry)
    {
        if (entry.Role != EncryptionCertificateRole.Active && _reportedPreviousKeyIds.TryAdd(entry.KeyId, true))
        {
            logger.ValueDecryptedWithPreviousCertificate(entry.KeyId);
        }
    }
}
