// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Tracks the principal and generation of locally open connections.
/// </summary>
public interface IConnectionOwnership
{
    /// <summary>
    /// Acquires the local lifecycle gate for a connection. Hold it through awaited side effects,
    /// but never for a stream's lifetime. All claim and subscription mutations require this gate.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="cancellationToken">Cancellation while waiting, not for cleanup.</param>
    /// <returns>A lease that releases the gate when disposed.</returns>
    ValueTask<IDisposable> Enter(ConnectionId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the local token for one observer subscription while holding the connection gate.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="key">The mediator's subscription key.</param>
    /// <returns>The new stream token.</returns>
    object Subscribe(ConnectionId id, object key);

    /// <summary>
    /// Checks whether a stream still owns its observer subscription under the connection gate.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="key">The mediator's subscription key.</param>
    /// <param name="token">The stream token.</param>
    /// <returns>Whether this is the current stream.</returns>
    bool IsCurrentSubscription(ConnectionId id, object key, object token);

    /// <summary>
    /// Releases only this stream's token after its cleanup, under the connection gate.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="key">The mediator's subscription key.</param>
    /// <param name="token">The stream token.</param>
    void ReleaseSubscription(ConnectionId id, object key, object token);

    /// <summary>
    /// Claims a connection, superseding only a claim by the same owner.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="owner">The calling principal.</param>
    /// <returns>A new claim, or null when denied.</returns>
    ConnectionClaim? Claim(ConnectionId id, ConnectionOwner owner);

    /// <summary>
    /// Authorizes access to a connection.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="owner">The calling principal.</param>
    /// <returns>The access decision.</returns>
    ConnectionAccess Authorize(ConnectionId id, ConnectionOwner owner);

    /// <summary>
    /// Checks whether a connection belongs to a principal.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="owner">The principal.</param>
    /// <returns>Whether the principal owns the connection.</returns>
    bool IsOwnedBy(ConnectionId id, ConnectionOwner owner);

    /// <summary>
    /// Gets the current local claim.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <returns>The claim, or null if not locally owned.</returns>
    ConnectionClaim? GetClaim(ConnectionId id);

    /// <summary>
    /// Checks whether a claim is still current.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="claim">The claim to check.</param>
    /// <returns>Whether the claim is current.</returns>
    bool IsCurrent(ConnectionId id, ConnectionClaim claim);

    /// <summary>
    /// Releases only the current claim.
    /// </summary>
    /// <param name="id">The connection identifier.</param>
    /// <param name="claim">The claim to release.</param>
    /// <returns>Whether the claim was released.</returns>
    bool Release(ConnectionId id, ConnectionClaim claim);
}
