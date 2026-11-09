// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_owner_is_unidentified_and_checked : a_reservation
{
    bool _result;

    async Task Establish() => await _reservation.Claim(Alice, _first, _token, Lease);
    async Task Because() => _result = await _reservation.IsHeldBy(ConnectionOwner.Unidentified);

    [Fact] void should_refuse_the_unidentified_owner() => _result.ShouldBeFalse();
}
