// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_owner_holds_the_connection_and_is_checked : a_reservation
{
    bool _result;

    async Task Establish() => await _reservation.Claim(Alice, _first, _token, Lease);
    async Task Because() => _result = await _reservation.IsHeldBy(Alice);

    [Fact] void should_confirm_the_owner() => _result.ShouldBeTrue();
}
