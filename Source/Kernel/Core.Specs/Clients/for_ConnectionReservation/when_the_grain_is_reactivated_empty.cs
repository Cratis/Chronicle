// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_grain_is_reactivated_empty : a_reservation
{
    bool _renewal;
    bool _other;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        _reservation = NewGrain();
    }

    async Task Because()
    {
        _renewal = await _reservation.Claim(Alice, _first, _token, Lease);
        _other = await _reservation.Claim(Bob, _second, _otherToken, Lease);
    }

    [Fact] void should_accept_the_renewal_of_the_holder() => _renewal.ShouldBeTrue();
    [Fact] void should_hold_the_connection_for_the_holder_again() => _other.ShouldBeFalse();
}
