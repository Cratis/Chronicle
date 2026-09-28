// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture;

public class when_disposing_readiness_client : Specification
{
    const string ConnectionString = "mongodb://localhost:27017/?directConnection=true";
    MongoClient _client;
    MongoClient _replacement;
    ICluster _originalCluster;
    ICluster _replacementCluster;

    void Establish()
    {
        _client = ReplicaSetMongoDBFixture.CreateReadinessClient(ConnectionString);
        _originalCluster = _client.Cluster;
    }

    void Because()
    {
        var settings = _client.Settings;
        ReplicaSetMongoDBFixture.DisposeReadinessClient(_client);
        _replacement = new MongoClient(settings);
        _replacementCluster = _replacement.Cluster;
    }

    void Destroy()
    {
        if (_replacement is not null)
        {
            ReplicaSetMongoDBFixture.DisposeReadinessClient(_replacement);
        }
    }

    [Fact] void should_unregister_the_cluster_from_the_driver_registry() => ReferenceEquals(_originalCluster, _replacementCluster).ShouldBeFalse();
}
