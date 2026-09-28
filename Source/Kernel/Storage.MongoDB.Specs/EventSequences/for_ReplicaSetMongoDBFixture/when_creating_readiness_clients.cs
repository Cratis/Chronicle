// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture;

public class when_creating_readiness_clients : Specification
{
    const string ConnectionString = "mongodb://localhost:27017/?directConnection=true";
    MongoClient _first;
    MongoClient _second;
    bool _useDifferentClusters;

    void Establish() => _first = ReplicaSetMongoDBFixture.CreateReadinessClient(ConnectionString);

    void Because()
    {
        _second = ReplicaSetMongoDBFixture.CreateReadinessClient(ConnectionString);
        _useDifferentClusters = !ReferenceEquals(_first.Cluster, _second.Cluster);
    }

    void Destroy()
    {
        ReplicaSetMongoDBFixture.DisposeReadinessClient(_first);
        if (_second is not null)
        {
            ReplicaSetMongoDBFixture.DisposeReadinessClient(_second);
        }
    }

    [Fact] void should_isolate_the_probe_cluster_from_other_clients() => _useDifferentClusters.ShouldBeTrue();
}
