// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Creates the connection a kernel dials the hosted federation authority it enrolled with over.
/// </summary>
public interface IFederationDialClientFactory
{
    /// <summary>
    /// Gets the serial number of the certificate a connection created now would present, or null when this kernel is not enrolled. It changes
    /// when the kernel renews, which is how a dialer knows to reconnect with the new certificate.
    /// </summary>
    /// <returns>The serial number, or null.</returns>
    Task<string?> GetCurrentSerial();

    /// <summary>
    /// Create a connection that presents the certificate obtained by enrollment and trusts the authority root that came with it.
    /// </summary>
    /// <param name="binding">The binding of the dialer link.</param>
    /// <returns>The connection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this kernel is not enrolled.</exception>
    Task<IFederationPullConnection> Create(FederationBindingState binding);
}
