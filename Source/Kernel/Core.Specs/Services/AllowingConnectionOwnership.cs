// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Creates <see cref="IConnectionOwnership"/> substitutes for specs that are not about who owns a connection.
/// </summary>
public static class AllowingConnectionOwnership
{
    /// <summary>
    /// Creates the <see cref="IConnectedClientsVisibility"/> the kernel uses, over an <see cref="IConnectionOwnership"/>.
    /// </summary>
    /// <param name="options">The <see cref="IOptions{ChronicleOptions}"/> to use.</param>
    /// <param name="ownership">The <see cref="IConnectionOwnership"/> to use.</param>
    /// <returns>The <see cref="IConnectedClientsVisibility"/>.</returns>
    public static IConnectedClientsVisibility VisibilityFor(IOptions<ChronicleOptions> options, IConnectionOwnership ownership) =>
        new ConnectedClientsVisibility(options, ownership, new HttpContextAccessor());

    /// <summary>
    /// Makes the cluster report only the given silos as active, so no connection is registered on another silo.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to stub.</param>
    /// <param name="silos">The silos that are active.</param>
    public static void WithActiveSilos(IGrainFactory grainFactory, params SiloAddress[] silos)
    {
        var management = Substitute.For<IManagementGrain>();
        management.GetHosts(true).Returns(silos.ToDictionary(silo => silo, _ => SiloStatus.Active));
        grainFactory.GetGrain<IManagementGrain>(Arg.Any<long>(), Arg.Any<string>()).Returns(management);
        WithAllowingReservations(grainFactory);
    }

    /// <summary>
    /// Makes every cluster-wide connection reservation succeed.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to stub.</param>
    public static void WithAllowingReservations(IGrainFactory grainFactory)
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.Claim(Arg.Any<ConnectionOwner>(), Arg.Any<SiloAddress>()).Returns(true);
        reservation.Release(Arg.Any<ConnectionOwner>(), Arg.Any<SiloAddress>()).Returns(Task.CompletedTask);
        grainFactory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(reservation);
    }

    /// <summary>
    /// Creates an <see cref="IConnectionOwnership"/> that says every caller owns every connection.
    /// </summary>
    /// <returns>The <see cref="IConnectionOwnership"/>.</returns>
    public static IConnectionOwnership Create()
    {
        var ownership = Substitute.For<IConnectionOwnership>();
        ownership.Claim(Arg.Any<ConnectionId>(), Arg.Any<ConnectionOwner>()).Returns(ConnectionClaim.Claimed);
        ownership.IsOwnedBy(Arg.Any<string>(), Arg.Any<ConnectionOwner>()).Returns(true);
        return ownership;
    }
}
