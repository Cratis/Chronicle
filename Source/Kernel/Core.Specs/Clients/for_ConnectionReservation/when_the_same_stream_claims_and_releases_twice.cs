// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_the_same_stream_claims_and_releases_twice : a_reservation
{
    bool _result;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        await _reservation.Claim(Alice, _first, _token, Lease);
        await _reservation.Release(Alice, _token);
        _clock.Advance(ConnectionOwnership.ReleasedClaimRetention);
    }

    async Task Because() => _result = await _reservation.Claim(Bob, _second, _otherToken, Lease);

    [Fact] void should_count_the_stream_once() => _result.ShouldBeTrue();
}
