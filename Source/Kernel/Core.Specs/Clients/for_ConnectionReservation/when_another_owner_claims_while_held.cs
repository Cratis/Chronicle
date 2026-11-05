// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_another_owner_claims_while_held : a_reservation
{
    bool _result;

    async Task Establish() => await _reservation.Claim(Alice, _first);

    async Task Because() => _result = await _reservation.Claim(Bob, _second);

    [Fact] void should_refuse_the_other_owner() => _result.ShouldBeFalse();
}
