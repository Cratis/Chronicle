// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBClusterHealthCheck.when_checking;

public class and_the_pool_is_saturated : given.a_cluster_health_check
{
    void Establish()
    {
        Describe(Server(ServerType.ReplicaSetPrimary));
        _client.GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>()).Throws(new MongoWaitQueueFullException("pool is full"));
        _client.ClearReceivedCalls();
    }

    async Task Because() => _result = await _check.CheckHealthAsync(new HealthCheckContext());
    [Fact] void should_remain_healthy() => _result.Status.ShouldEqual(HealthStatus.Healthy);
    [Fact] void should_only_read_cluster_state() => _client.ReceivedCalls().Select(_ => _.GetMethodInfo().Name).ShouldEachConformTo(_ => _ == "get_Cluster");
    [Fact] void should_not_get_a_database_or_run_a_command() => _client.DidNotReceive().GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>());
}
