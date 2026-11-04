// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation;

[Collection(MongoDBCollection.Name)]
public class when_moving_to_the_configured_location(MongoDBFixture fixture) : given.a_system_event_store_relocation(fixture)
{
    async Task Establish()
    {
        await Insert("System+es", "namespaces", new BsonDocument("_id", "Default"), new BsonDocument("_id", "tenant"));
        await Insert("System+es", "event-types", new BsonDocument { { "_id", "registered-before" }, { "origin", "legacy" } }, new BsonDocument { { "_id", "only-in-legacy" }, { "origin", "legacy" } });
        await Collection("System+es", "event-types").Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(new BsonDocument("origin", 1), new CreateIndexOptions { Name = "by_origin" }));
        await Insert("System+es+Default", "event-log", new BsonDocument("_id", 0), new BsonDocument("_id", 1), new BsonDocument("_id", 2));
        await Insert("System+es+tenant", "event-log", new BsonDocument("_id", 0));
        await Insert("System", "users", new BsonDocument("_id", "a-user"));

        // Written by the running kernel before the patch got to run.
        await Insert("_system+es", "event-types", new BsonDocument { { "_id", "registered-before" }, { "origin", "current" } });
    }

    async Task Because() => await _relocation.MoveTo(SystemEventStoreLocation.Configured);

    [Fact] async Task should_leave_only_the_configured_databases() => (await Databases()).ShouldEqual(["_system", "_system+es", "_system+es+Default", "_system+es+tenant"]);
    [Fact] async Task should_move_the_default_namespace() => (await Collection("_system+es+Default", "event-log").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(3L);
    [Fact] async Task should_move_a_registered_namespace() => (await Collection("_system+es+tenant", "event-log").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(1L);
    [Fact] async Task should_move_the_read_models() => (await Collection("_system", "users").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(1L);
    [Fact] async Task should_move_the_registered_namespaces() => (await Collection("_system+es", "namespaces").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(2L);
    [Fact] async Task should_keep_a_document_the_running_kernel_wrote() => (await Origin("registered-before")).ShouldEqual("current");
    [Fact] async Task should_move_a_document_the_target_did_not_have() => (await Origin("only-in-legacy")).ShouldEqual("legacy");
    [Fact] async Task should_move_the_indexes() => (await (await Collection("_system+es", "event-types").Indexes.ListAsync()).ToListAsync()).Select(_ => _["name"].AsString).ShouldContain("by_origin");

    async Task<string> Origin(string id) =>
        (await Collection("_system+es", "event-types").Find(new BsonDocument("_id", id)).SingleAsync())["origin"].AsString;
}
