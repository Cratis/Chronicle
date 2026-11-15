// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_a_stream_is_released_twice : a_reservation
{
    bool _result;

    async Task Establish()
    {
        await _reservation.Claim(Alice, _first, _token, Lease);
        await _reservation.Claim(Alice, _first, _otherToken, Lease);
        await _reservation.Release(Alice, _token);
        await _reservation.Release(Alice, _token);
    }

    async Task Because() => _result = await _reservation.Claim(Bob, _second, Guid.NewGuid(), Lease);

    [Fact] void should_not_release_the_other_stream() => _result.ShouldBeFalse();
}
