// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_authorizing;

public class with_another_owner : Specification
{
    readonly ConnectionOwnership _ownership = new();
    ConnectionAccess _access;

    void Establish() => _ownership.Claim("connection", new("original"));
    void Because() => _access = _ownership.Authorize("connection", new("other"));

    [Fact] void should_deny_access() => _access.ShouldEqual(ConnectionAccess.Denied);
}
