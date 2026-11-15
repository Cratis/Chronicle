// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_claiming;

public class as_unidentified : given.a_connection_ownership
{
    ConnectionClaim _result;

    void Because() => _result = _ownership.Claim(Id, ConnectionOwner.Unidentified);

    [Fact] void should_refuse_it() => _result.ShouldEqual(ConnectionClaim.Refused);
    [Fact] void should_not_own_it() => _ownership.IsOwnedBy(Id, ConnectionOwner.Unidentified).ShouldBeFalse();
}
