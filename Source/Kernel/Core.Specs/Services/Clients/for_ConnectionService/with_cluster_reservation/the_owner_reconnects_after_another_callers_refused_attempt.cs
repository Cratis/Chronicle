// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_owner_reconnects_after_another_callers_refused_attempt : given.a_two_silo_cluster
{
    Exception _refusal;
    Exception _result;

    async Task Establish()
    {
        using var cancellation = new CancellationTokenSource();
        await Open(_serviceA, AuthenticatedCalls.As("alice", cancellationToken: cancellation.Token));
        _refusal = await Open(_serviceB, AuthenticatedCalls.As("bob"));
        await cancellation.CancelAsync();
        await Released();
    }

    async Task Because() => _result = await Open(_serviceB, AuthenticatedCalls.As("alice"));

    [Fact] void should_refuse_the_other_caller() => ((RpcException)_refusal).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_serve_the_reconnecting_owner() => _result.ShouldBeNull();
    [Fact] void should_own_the_connection_on_the_second_silo() => Owns(_ownershipB, "alice").ShouldBeTrue();
    [Fact] void should_not_leave_the_other_caller_owning_the_connection() => Owns(_ownershipB, "bob").ShouldBeFalse();
}
