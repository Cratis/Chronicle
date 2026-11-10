// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_a_connection_nobody_holds_is_checked : a_reservation
{
    bool _result;
    bool _claimed;

    async Task Because()
    {
        _result = await _reservation.IsHeldBy(Alice);
        _claimed = await _reservation.Claim(Bob, _second, _otherToken, Lease);
    }

    [Fact] void should_not_confirm_an_owner() => _result.ShouldBeFalse();
    [Fact] void should_not_reserve_the_identifier_or_start_retention() => _claimed.ShouldBeTrue();
}
