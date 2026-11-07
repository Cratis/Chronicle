// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_claiming;

public class an_id_another_owner_released_within_retention : given.a_connection_ownership
{
    ConnectionClaim _result;

    void Establish()
    {
        _ownership.Claim(Id, Alice);
        _ownership.Release(Id, Alice);
        _clock.Advance(ConnectionOwnership.ReleasedClaimRetention - TimeSpan.FromSeconds(1));
    }

    void Because() => _result = _ownership.Claim(Id, Bob);

    [Fact] void should_refuse_it() => _result.ShouldEqual(ConnectionClaim.Refused);
    [Fact] void should_not_make_the_other_owner_an_owner() => _ownership.IsOwnedBy(Id, Bob).ShouldBeFalse();
    [Fact] void should_let_the_original_owner_claim_it_again() => _ownership.Claim(Id, Alice).ShouldEqual(ConnectionClaim.Claimed);
}
