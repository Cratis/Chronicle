// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

public class when_the_local_silo_holds_the_connection : given.a_connection_check
{
    void Establish() => _ownership.Claim(Id, Alice);
    async Task Because() => _result = await _ownership.Verify(_factory, Id, Alice, Timeout);

    [Fact] void should_grant_access() => _result.ShouldEqual(ConnectionAccessOutcome.Granted);
    [Fact] void should_not_reach_the_cluster() => _factory.ReceivedCalls().ShouldBeEmpty();
}
