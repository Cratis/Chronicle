// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines a system for reserving a connection identifier for one owner across the whole cluster.
/// </summary>
/// <remarks>
/// The grain is keyed by the connection identifier. It complements <see cref="IConnectionOwnership"/>, which only
/// knows the connections opened on its own silo. Each connection stream holds a lease identified by a token, and the
/// holder has to renew the lease to keep it.
/// </remarks>
[Version(2)]
[Alias("Cratis.Chronicle.Clients.IConnectionReservation")]
public interface IConnectionReservation : IGrainWithStringKey
{
    /// <summary>
    /// Claims the connection identifier for an owner on behalf of a silo, or renews the lease of a claim already made.
    /// </summary>
    /// <param name="owner">The <see cref="ConnectionOwner"/> claiming it.</param>
    /// <param name="silo">The <see cref="SiloAddress"/> of the silo holding the connection.</param>
    /// <param name="token">Identifies the connection stream. Claiming again with the same token renews the lease.</param>
    /// <param name="lease">How long the lease lasts from now unless it is renewed.</param>
    /// <returns>True when the owner holds the connection identifier; false when it is not available to the owner.</returns>
    Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease);

    /// <summary>
    /// Checks whether the owner still holds at least one active lease, without acquiring or renewing one.
    /// </summary>
    /// <param name="owner">The caller's identity.</param>
    /// <returns>True when the owner holds an active lease on an active silo; otherwise false.</returns>
    Task<bool> IsHeldBy(ConnectionOwner owner);

    /// <summary>
    /// Releases the claim of a connection stream. Releasing a claim that is not held has no effect.
    /// </summary>
    /// <param name="owner">The <see cref="ConnectionOwner"/> releasing it.</param>
    /// <param name="token">Identifies the connection stream.</param>
    /// <returns>An awaitable task.</returns>
    Task Release(ConnectionOwner owner, Guid token);
}
