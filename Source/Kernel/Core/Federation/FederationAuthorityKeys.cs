// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationAuthorityKeys"/> that holds the authority as a kernel-internal
/// secret of the system event store: the certificate as its public part and the PKCS#8 private key, wrapped by the key-encryption key,
/// as its private part. The key is never stored in the clear.
/// </summary>
/// <param name="keys">The <see cref="IEncryptionKeyStorage"/> that is the kernel's key custody.</param>
/// <param name="protection">The <see cref="IFederationKeyProtection"/> that wraps the private key.</param>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the hosted federation configuration.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> to date a new authority with.</param>
[Singleton]
public sealed class FederationAuthorityKeys(IEncryptionKeyStorage keys, IFederationKeyProtection protection, IOptions<ChronicleOptions> options, TimeProvider timeProvider) : IFederationAuthorityKeys, IDisposable
{
    const string Purpose = "authority";
    const string Identity = "federation-authority";

    static readonly TimeSpan _authorityLifetime = TimeSpan.FromDays(3650);
    readonly SemaphoreSlim _lock = new(1, 1);
    X509Certificate2? _authority;

    /// <inheritdoc/>
    public void Dispose()
    {
        _lock.Dispose();
        _authority?.Dispose();
    }

    /// <inheritdoc/>
    public async Task<(X509Certificate2 Certificate, bool Created)> GetOrCreate()
    {
        await _lock.WaitAsync();
        try
        {
            if (_authority is not null)
            {
                return (_authority, false);
            }

            var now = timeProvider.GetUtcNow();
            using var created = FederationCertificateAuthority.CreateAuthority(options.Value.Federation.Hosted.TrustDomain, now.AddMinutes(-5), now.Add(_authorityLifetime));
            using var privateKey = created.GetECDsaPrivateKey()!;
            var plaintext = privateKey.ExportPkcs8PrivateKey();
            EncryptionKey candidate;
            try
            {
                candidate = new EncryptionKey(created.RawData, await protection.Protect(plaintext, Purpose, Identity, created.SerialNumber));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }

            var stored = await keys.GetOrAddInternalSecretFor(EventStoreName.System, InternalSecretPurpose.FederationAuthority, candidate);
            var wasCreated = stored.Public.AsSpan().SequenceEqual(candidate.Public);

            using var certificate = X509CertificateLoader.LoadCertificate(stored.Public);
            var pkcs8 = await protection.Unprotect(stored.Private, Purpose, Identity, certificate.SerialNumber);
#pragma warning disable CA2000 // The key is owned by the authority certificate for the life of the process.
            var key = ECDsa.Create();
#pragma warning restore CA2000
            try
            {
                key.ImportPkcs8PrivateKey(pkcs8, out _);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pkcs8);
            }

            _authority = certificate.CopyWithPrivateKey(key);
            return (_authority, wasCreated);
        }
        finally
        {
            _lock.Release();
        }
    }
}
