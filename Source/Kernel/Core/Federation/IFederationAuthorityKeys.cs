// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the custody of the federation certificate authority key. The private key lives in the kernel's key custody
/// (decisions 0025 and 0028) and never in configuration, logs, events or audit entries.
/// </summary>
public interface IFederationAuthorityKeys
{
    /// <summary>
    /// Get the authority, creating it when none exists. Concurrent callers, on any silo, converge on one authority.
    /// </summary>
    /// <returns>The authority certificate with its private key, and whether this call created it.</returns>
    /// <exception cref="Storage.Compliance.InternalSecretsNotSupported">Thrown when the configured key store cannot hold kernel-internal secrets.</exception>
    Task<(X509Certificate2 Certificate, bool Created)> GetOrCreate();
}
