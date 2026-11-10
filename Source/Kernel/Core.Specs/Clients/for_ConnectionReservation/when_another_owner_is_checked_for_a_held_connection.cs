// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionReservation;

public class when_another_owner_is_checked_for_a_held_connection : a_reservation
{
    bool _bob;
    bool _issuer;
    bool _client;

    async Task Establish() => await _reservation.Claim(Alice, _first, _token, Lease);
    async Task Because()
    {
        _bob = await _reservation.IsHeldBy(Bob);
        _issuer = await _reservation.IsHeldBy(new("other", Alice.Subject));
        _client = await _reservation.IsHeldBy(Alice with { IsClient = true });
    }

    [Fact] void should_refuse_another_subject() => _bob.ShouldBeFalse();
    [Fact] void should_refuse_another_issuer() => _issuer.ShouldBeFalse();
    [Fact] void should_refuse_a_client_with_the_same_subject() => _client.ShouldBeFalse();
}
