// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_claiming;

public class with_another_owner : Specification
{
    readonly ConnectionOwnership _ownership = new();
    readonly ConnectionOwner _original = new("original");
    ConnectionClaim? _claim;

    void Establish() => _ownership.Claim("connection", _original);
    void Because() => _claim = _ownership.Claim("connection", new("other"));

    [Fact] void should_reject_the_claim() => _claim.ShouldBeNull();
    [Fact] void should_preserve_the_owner() => _ownership.IsOwnedBy("connection", _original).ShouldBeTrue();
}
