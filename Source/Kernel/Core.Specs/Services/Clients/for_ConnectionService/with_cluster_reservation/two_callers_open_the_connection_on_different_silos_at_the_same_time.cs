// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class two_callers_open_the_connection_on_different_silos_at_the_same_time : given.a_two_silo_cluster
{
    Exception?[] _results = [];

    async Task Because() => _results = await Task.WhenAll(
        Open(_serviceA, AuthenticatedCalls.As("alice")),
        Open(_serviceB, AuthenticatedCalls.As("bob")));

    [Fact] void should_serve_exactly_one_of_them() => _results.Count(_ => _ is null).ShouldEqual(1);
    [Fact] void should_refuse_the_other() => _results.Count(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldEqual(1);
}
