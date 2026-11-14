// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationEnrolledIdentities"/> that records the identity in the system event log
/// with its private key encrypted by AES-256-GCM under a kernel-internal secret held by the key custody.
/// </summary>
/// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding the recorded identity.</param>
/// <param name="keys">The <see cref="IEncryptionKeyStorage"/> that is the kernel's key custody.</param>
[Singleton]
public sealed class FederationEnrolledIdentities(IFederationCertificateRegistry registry, IEncryptionKeyStorage keys) : IFederationEnrolledIdentities, IDisposable
{
    const string Slot = "identity";
    const int NonceSize = 12;
    const int TagSize = 16;
    static readonly byte[] _associatedData = Encoding.UTF8.GetBytes("chronicle-federation-identity-v1");

    readonly SemaphoreSlim _lock = new(1, 1);
    FederationEnrolledIdentity? _cached;

    /// <inheritdoc/>
    public void Dispose() => _lock.Dispose();

    /// <inheritdoc/>
    public async Task<FederationEnrolledIdentity?> Get()
    {
        var installed = await registry.GetInstalledIdentity();
        if (installed is null)
        {
            return null;
        }

        await _lock.WaitAsync();
        try
        {
            if (_cached?.Serial == installed.Serial)
            {
                return _cached;
            }

            var secret = await GetProtectionSecret();
            var pkcs8 = Unprotect(Convert.FromBase64String(installed.ProtectedPrivateKey), secret);
#pragma warning disable CA2000 // The key is owned by the certificate it is attached to, which outlives this call.
            var key = ECDsa.Create();
#pragma warning restore CA2000
            key.ImportPkcs8PrivateKey(pkcs8, out _);
            CryptographicOperations.ZeroMemory(pkcs8);

            using var certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(installed.Certificate));
            _cached = new(
                certificate.CopyWithPrivateKey(key),
                X509CertificateLoader.LoadCertificate(Convert.FromBase64String(installed.Authority)),
                installed.Identity,
                installed.Serial,
                installed.NotBefore,
                installed.NotAfter,
                installed.Endpoint,
                installed.AuthorityFingerprint);
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task Install(ECDsa key, X509Certificate2 certificate, X509Certificate2 authority, string endpoint, string reason)
    {
        var secret = await GetProtectionSecret();
        var pkcs8 = key.ExportPkcs8PrivateKey();
        try
        {
            var identity = FederationCertificateAuthority.GetIdentity(certificate) ?? string.Empty;
            await registry.Append(Slot, new FederationIdentityInstalled(
                identity,
                certificate.SerialNumber,
                new DateTimeOffset(certificate.NotBefore),
                new DateTimeOffset(certificate.NotAfter),
                endpoint,
                FederationCertificateAuthority.Fingerprint(authority),
                Convert.ToBase64String(authority.RawData),
                Convert.ToBase64String(certificate.RawData),
                Convert.ToBase64String(Protect(pkcs8, secret)),
                reason));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }

    static byte[] Protect(byte[] plaintext, byte[] secret)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(secret, TagSize);
        aes.Encrypt(nonce, plaintext, cipher, tag, _associatedData);
        return [.. nonce, .. tag, .. cipher];
    }

    static byte[] Unprotect(byte[] protectedBytes, byte[] secret)
    {
        var nonce = protectedBytes.AsSpan(0, NonceSize);
        var tag = protectedBytes.AsSpan(NonceSize, TagSize);
        var cipher = protectedBytes.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[cipher.Length];
        using var aes = new AesGcm(secret, TagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext, _associatedData);
        return plaintext;
    }

    async Task<byte[]> GetProtectionSecret()
    {
        var candidate = new EncryptionKey(RandomNumberGenerator.GetBytes(16), RandomNumberGenerator.GetBytes(32));
        var stored = await keys.GetOrAddInternalSecretFor(EventStoreName.System, InternalSecretPurpose.FederationKeyProtection, candidate);
        return stored.Private;
    }
}
