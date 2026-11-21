// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the custody of the identity this kernel holds after it enrolled. The private key is only ever stored protected by
/// the operator's key-encryption key, never in plain configuration, logs or events.
/// </summary>
public interface IFederationEnrolledIdentities
{
    /// <summary>
    /// Get the identity this kernel holds.
    /// </summary>
    /// <returns>The identity, or null when this kernel is not enrolled.</returns>
    Task<FederationEnrolledIdentity?> Get();

    /// <summary>
    /// Store a new identity, replacing the previous one. It takes effect without a restart.
    /// </summary>
    /// <param name="key">The private key of the certificate.</param>
    /// <param name="certificate">The issued certificate.</param>
    /// <param name="authority">The authority certificate.</param>
    /// <param name="endpoint">The public endpoint of the authority.</param>
    /// <param name="reason">Why it is stored, see <see cref="FederationIssuanceReasons"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Install(ECDsa key, X509Certificate2 certificate, X509Certificate2 authority, string endpoint, string reason);
}
