// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_owner_was_released_and_is_checked : a_reservation
{
    bool _result;
    bool _bob;
    bool _alice;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        await _reservation.Release(Alice, _token);
    }
    async Task Because()
    {
        _result = await _reservation.IsHeldBy(Alice);
        _bob = await _reservation.Claim(Bob, _second, _otherToken, Lease);
        _alice = await _reservation.Claim(Alice, _second, _otherToken, Lease);
    }

    [Fact] void should_refuse_the_released_owner() => _result.ShouldBeFalse();
    [Fact] void should_preserve_retention() => _bob.ShouldBeFalse();
    [Fact] void should_allow_the_owner_to_reconnect() => _alice.ShouldBeTrue();
}
