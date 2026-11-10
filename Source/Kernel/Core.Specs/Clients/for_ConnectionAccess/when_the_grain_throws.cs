// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

public class when_the_grain_throws : given.a_connection_check
{
    void Establish() => _reservation.IsHeldBy(Alice).Returns<Task<bool>>(_ => throw new NotSupportedException("old silo"));
    async Task Because() => _result = await _ownership.Verify(_factory, Id, Alice, Timeout);

    [Fact] void should_report_that_access_cannot_be_verified() => _result.ShouldEqual(ConnectionAccessOutcome.Unverifiable);
}
