// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_holding_silo_has_left_and_the_owner_is_checked : a_reservation
{
    bool _result;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        _active.Remove(_first);
    }
    async Task Because() => _result = await _reservation.IsHeldBy(Alice);

    [Fact] void should_refuse_the_owner_on_the_departed_silo() => _result.ShouldBeFalse();
}
