// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

public class when_the_owner_is_unidentified : given.a_connection_check
{
    async Task Because() => _result = await _ownership.Verify(_factory, Id, ConnectionOwner.Unidentified, Timeout);

    [Fact] void should_deny_access() => _result.ShouldEqual(ConnectionAccessOutcome.Denied);
    [Fact] void should_not_reach_the_cluster() => _factory.ReceivedCalls().ShouldBeEmpty();
}
