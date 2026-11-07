// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_lease_is_not_renewed : a_reservation
{
    bool _beforeTheRetention;
    bool _afterTheRetention;

    async Task Establish() => await _reservation.Claim(Alice, _first, _token, Lease);

    async Task Because()
    {
        _clock.Advance(Lease + ConnectionOwnership.ReleasedClaimRetention - TimeSpan.FromSeconds(1));
        _beforeTheRetention = await _reservation.Claim(Bob, _second, _otherToken, Lease);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _afterTheRetention = await _reservation.Claim(Bob, _second, _otherToken, Lease);
    }

    [Fact] void should_count_the_retention_from_when_the_lease_lapsed() => _beforeTheRetention.ShouldBeFalse();
    [Fact] void should_allow_the_other_owner_once_the_retention_has_passed() => _afterTheRetention.ShouldBeTrue();
}
