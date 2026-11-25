// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_revoking;

public class the_last_stream : given.a_connection_ownership
{
    ConnectionClaim _otherCallersClaim;

    void Establish() => _ownership.Claim(Id, Alice);

    void Because()
    {
        _ownership.Revoke(Id, Alice);
        _clock.Advance(TimeSpan.FromMinutes(1));
        _otherCallersClaim = _ownership.Claim(Id, Bob);
    }

    [Fact] void should_no_longer_own_the_connection() => _ownership.IsOwnedBy(Id, Alice).ShouldBeFalse();
    [Fact] void should_not_keep_the_claim_as_a_released_one_that_locks_others_out() => _otherCallersClaim.ShouldEqual(ConnectionClaim.Claimed);
}
