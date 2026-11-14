// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationEnrolledIdentities"/> that records the identity in the system event log
/// with its private key wrapped by the key-encryption key, bound to the identity and the serial number of its certificate.
/// </summary>
/// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding the recorded identity.</param>
/// <param name="protection">The <see cref="IFederationKeyProtection"/> that wraps the private key.</param>
[Singleton]
public sealed class FederationEnrolledIdentities(IFederationCertificateRegistry registry, IFederationKeyProtection protection) : IFederationEnrolledIdentities, IDisposable
{
    const string Slot = "identity";
    const string Purpose = "identity";

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

            var pkcs8 = await protection.Unprotect(Convert.FromBase64String(installed.ProtectedPrivateKey), Purpose, installed.Identity, installed.Serial);
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
        var pkcs8 = key.ExportPkcs8PrivateKey();
        try
        {
            var identity = FederationCertificateAuthority.GetIdentity(certificate) ?? string.Empty;
            var wrapped = await protection.Protect(pkcs8, Purpose, identity, certificate.SerialNumber);
            await registry.Append(Slot, new FederationIdentityInstalled(
                identity,
                certificate.SerialNumber,
                new DateTimeOffset(certificate.NotBefore),
                new DateTimeOffset(certificate.NotAfter),
                endpoint,
                FederationCertificateAuthority.Fingerprint(authority),
                Convert.ToBase64String(authority.RawData),
                Convert.ToBase64String(certificate.RawData),
                Convert.ToBase64String(wrapped),
                reason));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }
}
