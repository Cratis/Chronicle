// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_reservation_throws : given.a_two_silo_cluster
{
    Exception _result;
    IConnectionReservation _failing;

    protected override IConnectionReservation Decorate(IConnectionReservation grain)
    {
        var reservation = _failing = Substitute.For<IConnectionReservation>();
        reservation.Claim(Arg.Any<ConnectionOwner>(), Arg.Any<SiloAddress>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>()).Returns<Task<bool>>(_ => throw new InvalidOperationException("unavailable"));
        return reservation;
    }

    async Task Because()
    {
        _result = await Open(_serviceA, AuthenticatedCalls.As("alice"));
        await KeepAlivesReached(2);
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        while (Claims < 2 && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }

    int Claims => _failing.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IConnectionReservation.Claim));

    [Fact] void should_serve_the_client_without_the_reservation() => _result.ShouldBeNull();
    [Fact] void should_hold_the_connection_locally() => Owns(_ownershipA, "alice").ShouldBeTrue();
    [Fact] void should_try_to_verify_again_at_the_next_keep_alive() => Claims.ShouldBeGreaterThanOrEqual(2);
}
