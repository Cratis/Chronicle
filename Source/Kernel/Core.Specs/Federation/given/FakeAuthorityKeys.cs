// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// An <see cref="IFederationAuthorityKeys"/> holding one authority in memory.
/// </summary>
public class FakeAuthorityKeys : IFederationAuthorityKeys
{
    X509Certificate2? _authority;

    /// <summary>
    /// Gets the authority, creating it on first use.
    /// </summary>
    public X509Certificate2 Authority => _authority ??= FederationCertificateAuthority.CreateAuthority("example", DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow.AddYears(20));

    /// <inheritdoc/>
    public Task<(X509Certificate2 Certificate, bool Created)> GetOrCreate()
    {
        var created = _authority is null;
        return Task.FromResult((Authority, created));
    }
}
