// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBClusterHealthCheck.when_checking;

public class and_no_server_is_connected : given.a_cluster_health_check
{
    const string HeartbeatFailure = "The MongoDB heartbeat failed";

    void Establish() => Describe(Server(ServerType.Unknown, ServerState.Disconnected, new IOException(HeartbeatFailure)));
    async Task Because() => _result = await _check.CheckHealthAsync(new HealthCheckContext());
    [Fact] void should_be_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_describe_the_heartbeat_failure() => _result.Description.ShouldContain(HeartbeatFailure);
}
