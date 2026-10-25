// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBIndexProvisioner;

[Collection(MongoDBCollection.Name)]
public class when_pruning_obsolete_event_indexes(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    MongoDBIndexReport _before;
    MongoDBIndexReport _after;
    string _obsolete;

    async Task Establish()
    {
        _obsolete = EventStoreNamespaceDatabase.ObsoleteEventIndexNames[0];
        await _namespace.GetCollection<BsonDocument>(WellKnownCollectionNames.EventLog).Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(new BsonDocument("legacy", 1), new CreateIndexOptions { Name = _obsolete }));
        _before = await _provisioner.Verify([Store]);
    }

    async Task Because() => _after = await _provisioner.Migrate([Store]);

    [Fact] void should_report_obsolete_indexes_without_dropping_them() => _before.Scopes.Any(scope => scope.Obsolete.Contains($"event-log/{_obsolete}")).ShouldBeTrue();
    [Fact] void should_remove_obsolete_indexes_during_migration() => _after.Scopes.All(scope => scope.Obsolete.Count == 0).ShouldBeTrue();
    [Fact] async Task should_keep_the_id_index() => (await Indexes(_namespace, "event-log")).Any(index => index["name"] == "_id_").ShouldBeTrue();
}
