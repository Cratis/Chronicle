// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_reservation_throws : given.a_two_silo_cluster
{
    Exception _result;

    protected override IConnectionReservation CreateReservation(IClusterMembershipService membership)
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.Claim(Arg.Any<ConnectionOwner>(), Arg.Any<SiloAddress>()).Returns<Task<bool>>(_ => throw new InvalidOperationException("unavailable"));
        return reservation;
    }

    async Task Because() => _result = await Open(_serviceA, AuthenticatedCalls.As("alice"));

    [Fact] void should_be_refused() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_release_the_local_claim() => Owns(_ownershipA, "alice").ShouldBeFalse();
}
