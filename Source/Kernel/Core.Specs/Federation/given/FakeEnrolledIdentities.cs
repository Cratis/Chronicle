// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// An <see cref="IFederationEnrolledIdentities"/> that keeps the identity in memory.
/// </summary>
public class FakeEnrolledIdentities : IFederationEnrolledIdentities
{
    FederationEnrolledIdentity? _identity;

    /// <summary>
    /// Gets the reasons the identity was installed, in order.
    /// </summary>
    public List<string> Reasons { get; } = [];

    /// <inheritdoc/>
    public Task<FederationEnrolledIdentity?> Get() => Task.FromResult(_identity);

    /// <inheritdoc/>
    public Task Install(ECDsa key, X509Certificate2 certificate, X509Certificate2 authority, string endpoint, string reason)
    {
        var withKey = certificate.CopyWithPrivateKey(ECDsa.Create(key.ExportParameters(true)));
        _identity = new(
            withKey,
            X509CertificateLoader.LoadCertificate(authority.RawData),
            FederationCertificateAuthority.GetIdentity(certificate) ?? string.Empty,
            certificate.SerialNumber,
            new DateTimeOffset(certificate.NotBefore),
            new DateTimeOffset(certificate.NotAfter),
            endpoint,
            FederationCertificateAuthority.Fingerprint(authority));
        Reasons.Add(reason);
        return Task.CompletedTask;
    }
}
