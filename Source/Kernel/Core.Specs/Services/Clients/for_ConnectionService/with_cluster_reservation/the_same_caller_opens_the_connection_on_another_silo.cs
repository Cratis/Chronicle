// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_same_caller_opens_the_connection_on_another_silo : given.a_two_silo_cluster
{
    Exception _result;

    async Task Establish() => await Open(_serviceA, AuthenticatedCalls.As("alice"));

    async Task Because() => _result = await Open(_serviceB, AuthenticatedCalls.As("alice"));

    [Fact] void should_be_served() => _result.ShouldBeNull();
    [Fact] void should_own_the_connection_on_the_second_silo() => Owns(_ownershipB, "alice").ShouldBeTrue();
}
