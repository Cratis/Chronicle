// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

public class when_the_local_silo_misses_and_the_grain_confirms : given.a_connection_check
{
    void Establish() => _reservation.IsHeldBy(Alice).Returns(true);
    async Task Because() => _result = await _ownership.Verify(_factory, Id, Alice, Timeout);

    [Fact] void should_grant_access() => _result.ShouldEqual(ConnectionAccessOutcome.Granted);
    [Fact] async Task should_check_the_caller_once() => await _reservation.Received(1).IsHeldBy(Alice);
    [Fact] async Task should_never_claim() => await _reservation.DidNotReceive().Claim(Arg.Any<ConnectionOwner>(), Arg.Any<SiloAddress>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>());
    [Fact] void should_not_acquire_local_ownership() => _ownership.IsOwnedBy(Id, Alice).ShouldBeFalse();
}
