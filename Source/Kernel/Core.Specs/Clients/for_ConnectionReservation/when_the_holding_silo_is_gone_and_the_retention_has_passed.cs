// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_holding_silo_is_gone_and_the_retention_has_passed : a_reservation
{
    bool _result;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        _active.Remove(_first);
        await _reservation.Claim(Bob, _second, _otherToken, Lease);
        _clock.Advance(ConnectionOwnership.ReleasedClaimRetention);
    }

    async Task Because() => _result = await _reservation.Claim(Bob, _second, _otherToken, Lease);

    [Fact] void should_drop_the_dead_silos_claim_and_allow_the_other_owner() => _result.ShouldBeTrue();
}
