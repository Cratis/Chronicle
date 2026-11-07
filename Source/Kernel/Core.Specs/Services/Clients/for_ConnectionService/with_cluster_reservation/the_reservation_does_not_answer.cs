// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_reservation_does_not_answer : given.a_two_silo_cluster
{
    Exception _result;

    protected override IConnectionReservation Decorate(IConnectionReservation grain)
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.Claim(Arg.Any<ConnectionOwner>(), Arg.Any<SiloAddress>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>()).Returns(new TaskCompletionSource<bool>().Task);
        return reservation;
    }

    async Task Because() => _result = await Open(_serviceA, AuthenticatedCalls.As("alice"));

    [Fact] void should_report_that_the_connection_could_not_be_verified() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_release_the_local_claim() => Owns(_ownershipA, "alice").ShouldBeFalse();
    [Fact] async Task should_undo_the_claim_that_may_have_been_made() => await Released();
}
