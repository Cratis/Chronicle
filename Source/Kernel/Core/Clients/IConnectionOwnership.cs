// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines which caller opened each client connection on this silo.
/// </summary>
public interface IConnectionOwnership
{
    /// <summary>
    /// Claims a connection identifier for an owner.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> to claim.</param>
    /// <param name="owner">The <see cref="ConnectionOwner"/> claiming it.</param>
    /// <returns>Whether the claim was made.</returns>
    ConnectionClaim Claim(ConnectionId connectionId, ConnectionOwner owner);

    /// <summary>
    /// Releases one claim an owner holds on a connection identifier.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> to release.</param>
    /// <param name="owner">The <see cref="ConnectionOwner"/> releasing it.</param>
    void Release(ConnectionId connectionId, ConnectionOwner owner);

    /// <summary>
    /// Checks whether an owner holds an open connection.
    /// </summary>
    /// <param name="connectionId">The connection identifier to check.</param>
    /// <param name="owner">The <see cref="ConnectionOwner"/> to check.</param>
    /// <returns>True if the owner holds the connection; false otherwise.</returns>
    bool IsOwnedBy(string connectionId, ConnectionOwner owner);

    /// <summary>
    /// Takes back a claim for a stream that turned out not to be the owner's to open, so that it leaves no trace.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Release"/>, which keeps the released claim for a while so that a replayed keep-alive from a
    /// departed owner is still recognized, this is for a stream refused after it was already served: a claim that is
    /// kept for an hour would lock out the real owner of the connection for that hour.
    /// </remarks>
    /// <param name="connectionId">The <see cref="ConnectionId"/> that was claimed.</param>
    /// <param name="owner">The <see cref="ConnectionOwner"/> that claimed it.</param>
    void Revoke(ConnectionId connectionId, ConnectionOwner owner);
}
