// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_a_non_owner_releases : a_reservation
{
    bool _result;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        await _reservation.Release(Bob, _token);
    }

    async Task Because() => _result = await _reservation.Claim(Bob, _second, _otherToken, Lease);

    [Fact] void should_not_release_the_owners_claim() => _result.ShouldBeFalse();
}
