// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Provides the named client certificates a consumer presents to producers. The provider owns the certificates; callers must not dispose them.
/// </summary>
public interface IFederationClientCertificates : IDisposable
{
    /// <summary>
    /// Get a named client certificate from the Federation:Certificates configuration.
    /// </summary>
    /// <param name="name">The certificate name, as referenced by a binding.</param>
    /// <returns>The certificate with its private key.</returns>
    /// <exception cref="InvalidOperationException">The name is unknown or its file is missing.</exception>
    X509Certificate2 Get(string name);
}
