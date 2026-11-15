// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_a_claim_arrives_after_its_release : a_reservation
{
    bool _result;
    bool _otherTokenGranted;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        await _reservation.Release(Alice, _token);
    }

    async Task Because()
    {
        _result = await _reservation.Claim(Alice, _first, _token, Lease);
        _otherTokenGranted = await _reservation.Claim(Alice, _first, _otherToken, Lease);
    }

    [Fact] void should_refuse_the_released_token() => _result.ShouldBeFalse();
    [Fact] void should_still_grant_another_token() => _otherTokenGranted.ShouldBeTrue();
}
