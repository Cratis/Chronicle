// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_another_owner_claims_after_the_retention : a_reservation
{
    bool _result;
    bool _previousOwner;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        await _reservation.Release(Alice, _token);
        _clock.Advance(ConnectionOwnership.ReleasedClaimRetention);
    }

    async Task Because()
    {
        _result = await _reservation.Claim(Bob, _second, _otherToken, Lease);
        _previousOwner = await _reservation.Claim(Alice, _first, _token, Lease);
    }

    [Fact] void should_allow_the_other_owner() => _result.ShouldBeTrue();
    [Fact] void should_refuse_the_previous_owner() => _previousOwner.ShouldBeFalse();
}
