// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Storage.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;

using context = Cratis.Chronicle.MongoDB.Integration.for_MongoDBKernelStateResetHandler.when_resetting_a_shared_server.context;

namespace Cratis.Chronicle.MongoDB.Integration.for_MongoDBKernelStateResetHandler;

[Collection(MongoDBCollection.Name)]
public class when_resetting_a_shared_server(context context) : MongoDBGiven<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_mongo_client(fixture)
    {
        readonly string _prefix = $"reset_{Guid.NewGuid().ToString("N")[..16]}_";
        string _owned;
        string _cluster;
        string _unrelated;

        public long OwnedDocuments;
        public long ClusterDocuments;
        public long UnrelatedDocuments;

        async Task Establish()
        {
            _owned = $"{_prefix}testing+es+Default";
            _cluster = $"{_prefix}{WellKnownDatabaseNames.Chronicle}";
            _unrelated = $"other_{Guid.NewGuid():N}";
            foreach (var name in new[] { _owned, _cluster, _unrelated })
            {
                await _client.GetDatabase(name).GetCollection<BsonDocument>("marker").InsertOneAsync(new BsonDocument("_id", "keep"));
            }
        }

        async Task Because()
        {
            var manager = Substitute.For<IMongoDBClientManager>();
            manager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
            var handler = new MongoDBKernelStateResetHandler(
                Options.Create(new Configuration.ChronicleOptions()),
                Options.Create(new MongoDBOptions { Server = MongoDBConnectionString }),
                manager,
                Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix }));
            await handler.Reset();
            OwnedDocuments = await Count(_owned);
            ClusterDocuments = await Count(_cluster);
            UnrelatedDocuments = await Count(_unrelated);
        }

        Task<long> Count(string name) => _client.GetDatabase(name).GetCollection<BsonDocument>("marker").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

        async Task Cleanup()
        {
            foreach (var name in new[] { _owned, _cluster, _unrelated })
            {
                await _client.DropDatabaseAsync(name);
            }
        }
    }

    [Fact] void should_remove_prefixed_event_data() => Context.OwnedDocuments.ShouldEqual(0L);
    [Fact] void should_preserve_prefixed_cluster_data() => Context.ClusterDocuments.ShouldEqual(1L);
    [Fact] void should_preserve_other_runs_data() => Context.UnrelatedDocuments.ShouldEqual(1L);
}
