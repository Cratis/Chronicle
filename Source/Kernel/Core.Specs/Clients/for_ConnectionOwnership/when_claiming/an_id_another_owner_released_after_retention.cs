// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_claiming;

public class an_id_another_owner_released_after_retention : given.a_connection_ownership
{
    ConnectionClaim _result;

    void Establish()
    {
        _ownership.Claim(Id, Alice);
        _ownership.Release(Id, Alice);
        _clock.Advance(ConnectionOwnership.ReleasedClaimRetention + TimeSpan.FromSeconds(1));
    }

    void Because() => _result = _ownership.Claim(Id, Bob);

    [Fact] void should_claim_it() => _result.ShouldEqual(ConnectionClaim.Claimed);
    [Fact] void should_be_owned_by_the_new_owner() => _ownership.IsOwnedBy(Id, Bob).ShouldBeTrue();
}
