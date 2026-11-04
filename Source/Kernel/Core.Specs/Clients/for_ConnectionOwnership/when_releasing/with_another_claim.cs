// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_releasing;

public class with_another_claim : Specification
{
    readonly ConnectionOwnership _ownership = new();
    readonly ConnectionOwner _original = new("original");
    bool _released;

    void Establish() => _ownership.Claim("connection", _original);
    void Because() => _released = _ownership.Release("connection", new ConnectionClaim(new("other")));

    [Fact] void should_reject_cleanup() => _released.ShouldBeFalse();
    [Fact] void should_preserve_the_owner() => _ownership.IsOwnedBy("connection", _original).ShouldBeTrue();
}
