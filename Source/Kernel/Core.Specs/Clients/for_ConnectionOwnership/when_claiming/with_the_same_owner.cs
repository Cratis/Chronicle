// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_claiming;

public class with_the_same_owner : Specification
{
    readonly ConnectionOwnership _ownership = new();
    readonly ConnectionOwner _owner = new("owner");
    ConnectionClaim _oldClaim;
    ConnectionClaim? _claim;

    void Establish() => _oldClaim = _ownership.Claim("connection", _owner)!;
    void Because() => _claim = _ownership.Claim("connection", _owner);

    [Fact] void should_issue_a_new_claim() => _claim.ShouldNotBeSame(_oldClaim);
    [Fact] void should_keep_the_registration_after_old_cleanup() => _ownership.Release("connection", _oldClaim).ShouldBeFalse();
    [Fact] void should_remain_owned() => _ownership.IsOwnedBy("connection", _owner).ShouldBeTrue();
}
