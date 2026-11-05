// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_another_owner_claims_within_the_retention : a_reservation
{
    bool _result;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first);
        await _reservation.Release(Alice, _first);
        _clock.Advance(ConnectionOwnership.ReleasedClaimRetention - TimeSpan.FromSeconds(1));
    }

    async Task Because() => _result = await _reservation.Claim(Bob, _second);

    [Fact] void should_refuse_the_other_owner() => _result.ShouldBeFalse();
}
