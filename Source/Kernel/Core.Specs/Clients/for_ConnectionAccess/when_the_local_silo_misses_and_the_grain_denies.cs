// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

public class when_the_local_silo_misses_and_the_grain_denies : given.a_connection_check
{
    async Task Because() => _result = await _ownership.Verify(_factory, Id, Alice, Timeout);

    [Fact] void should_deny_access() => _result.ShouldEqual(ConnectionAccessOutcome.Denied);
}
