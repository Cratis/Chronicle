// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_claiming;

public class an_id_the_same_owner_released : given.a_connection_ownership
{
    ConnectionClaim _result;

    void Establish()
    {
        _ownership.Claim(Id, Alice);
        _ownership.Release(Id, Alice);
    }

    void Because() => _result = _ownership.Claim(Id, Alice);

    [Fact] void should_claim_it() => _result.ShouldEqual(ConnectionClaim.Claimed);
    [Fact] void should_be_owned_again() => _ownership.IsOwnedBy(Id, Alice).ShouldBeTrue();
}
