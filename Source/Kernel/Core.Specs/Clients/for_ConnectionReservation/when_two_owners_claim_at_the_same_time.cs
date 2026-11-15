// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_two_owners_claim_at_the_same_time : a_reservation
{
    bool[] _results;

    async Task Because() => _results = await Task.WhenAll(
        _reservation.Claim(Alice, _first, _token, Lease),
        _reservation.Claim(Bob, _second, _otherToken, Lease));

    [Fact] void should_grant_exactly_one_of_them() => _results.Count(_ => _).ShouldEqual(1);
}
