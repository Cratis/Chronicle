// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBClusterHealthCheck.given;

public class a_cluster_health_check : Specification
{
    protected IMongoClient _client;
    protected ICluster _cluster;
    protected ClusterId _clusterId;
    protected MongoDBClusterHealthCheck _check;
    protected HealthCheckResult _result;

    void Establish()
    {
        _clusterId = new ClusterId();
        _cluster = Substitute.For<ICluster>();
        _client = Substitute.For<IMongoClient>();
        _client.Cluster.Returns(_cluster);
        var settings = new MongoClientSettings();
        var manager = Substitute.For<IMongoDBClientManager>();
        manager.GetClientFor(settings).Returns(_client);
        _check = new MongoDBClusterHealthCheck(manager, settings);
    }

    protected ServerDescription Server(ServerType type, ServerState state = ServerState.Connected, Exception? heartbeatException = null)
    {
        var endpoint = new DnsEndPoint("localhost", 27017);
        return new ServerDescription(new ServerId(_clusterId, endpoint), endpoint, heartbeatException: heartbeatException, state: state, type: type);
    }

    protected void Describe(params ServerDescription[] servers) =>
        _cluster.Description.Returns(new ClusterDescription(_clusterId, false, null, ClusterType.ReplicaSet, servers));
}
