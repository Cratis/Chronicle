// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines a system for reserving a connection identifier for one owner across the whole cluster.
/// </summary>
/// <remarks>
/// The grain is keyed by the connection identifier. It complements <see cref="IConnectionOwnership"/>, which only
/// knows the connections opened on its own silo.
/// </remarks>
[Version(1)]
[Alias("Cratis.Chronicle.Clients.IConnectionReservation")]
public interface IConnectionReservation : IGrainWithStringKey
{
    /// <summary>
    /// Claims the connection identifier for an owner on behalf of a silo.
    /// </summary>
    /// <param name="owner">The <see cref="ConnectionOwner"/> claiming it.</param>
    /// <param name="silo">The <see cref="SiloAddress"/> of the silo holding the connection.</param>
    /// <returns>True when the owner holds the connection identifier; false when it is not available to the owner.</returns>
    Task<bool> Claim(ConnectionOwner owner, SiloAddress silo);

    /// <summary>
    /// Releases one claim an owner holds on the connection identifier through a silo.
    /// </summary>
    /// <param name="owner">The <see cref="ConnectionOwner"/> releasing it.</param>
    /// <param name="silo">The <see cref="SiloAddress"/> of the silo that held the connection.</param>
    /// <returns>An awaitable task.</returns>
    Task Release(ConnectionOwner owner, SiloAddress silo);
}
