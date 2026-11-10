// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_reservation_lookup_times_out_during_a_keep_alive : given.a_two_silo_cluster
{
    Exception _result;
    protected override IConnectionReservation Decorate(IConnectionReservation grain)
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.IsHeldBy(Arg.Any<ConnectionOwner>()).Returns(new TaskCompletionSource<bool>().Task);
        return reservation;
    }
    async Task Because() => _result = await Catch.Exception(() => _serviceB.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = Id }, AuthenticatedCalls.As("alice")));

    [Fact] void should_report_that_verification_is_unavailable() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.Unavailable);
}
