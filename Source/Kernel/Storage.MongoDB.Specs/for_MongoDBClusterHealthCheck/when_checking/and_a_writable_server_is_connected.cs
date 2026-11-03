// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBClusterHealthCheck.when_checking;

public class and_a_writable_server_is_connected : given.a_cluster_health_check
{
    void Establish() => Describe(Server(ServerType.ReplicaSetPrimary));
    async Task Because() => _result = await _check.CheckHealthAsync(new HealthCheckContext());
    [Fact] void should_be_healthy() => _result.Status.ShouldEqual(HealthStatus.Healthy);
}
