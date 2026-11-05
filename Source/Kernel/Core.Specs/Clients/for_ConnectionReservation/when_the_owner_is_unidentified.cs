// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_owner_is_unidentified : a_reservation
{
    bool _result;

    async Task Because() => _result = await _reservation.Claim(ConnectionOwner.Unidentified, _first);

    [Fact] void should_refuse() => _result.ShouldBeFalse();
}
