// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_same_owner_claims_again : a_reservation
{
    bool _result;

    async Task Establish() => await _reservation.Claim(Alice, _first);

    async Task Because() => _result = await _reservation.Claim(Alice, _second);

    [Fact] void should_allow_the_owner() => _result.ShouldBeTrue();
}
