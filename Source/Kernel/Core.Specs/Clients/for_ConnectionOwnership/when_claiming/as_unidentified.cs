// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_claiming;

public class as_unidentified : Specification
{
    readonly ConnectionOwnership _ownership = new();
    ConnectionClaim? _claim;

    void Because() => _claim = _ownership.Claim("connection", ConnectionOwner.Unidentified);

    [Fact] void should_reject_the_claim() => _claim.ShouldBeNull();
    [Fact] void should_deny_access() => _ownership.Authorize("connection", ConnectionOwner.Unidentified).ShouldEqual(ConnectionAccess.Denied);
}
