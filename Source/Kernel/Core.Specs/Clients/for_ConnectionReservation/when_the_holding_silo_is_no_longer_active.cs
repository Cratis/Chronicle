// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_holding_silo_is_no_longer_active : a_reservation
{
    bool _result;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first);
        _active.Remove(_first);
    }

    async Task Because() => _result = await _reservation.Claim(Bob, _second);

    [Fact] void should_still_refuse_within_the_retention() => _result.ShouldBeFalse();
}
