// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_claiming;

public class an_unclaimed_id : given.a_connection_ownership
{
    ConnectionClaim _result;

    void Because() => _result = _ownership.Claim(Id, Alice);

    [Fact] void should_claim_it() => _result.ShouldEqual(ConnectionClaim.Claimed);
    [Fact] void should_be_owned_by_the_claimer() => _ownership.IsOwnedBy(Id, Alice).ShouldBeTrue();
    [Fact] void should_not_be_owned_by_anyone_else() => _ownership.IsOwnedBy(Id, Bob).ShouldBeFalse();
}
