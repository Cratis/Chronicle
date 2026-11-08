// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation;

/// <summary>
/// The first release of the move did nothing, was recorded as applied, and left the kernel running against empty
/// configured databases while the data stayed under the legacy names.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_the_kernel_already_ran_with_empty_configured_databases(MongoDBFixture fixture) : given.a_system_event_store_relocation(fixture)
{
    async Task Establish()
    {
        await Insert("System+es", "event-types", new BsonDocument("_id", "legacy-only"));
        await Insert("System+es+Default", "event-log", new BsonDocument("_id", 0), new BsonDocument("_id", 1));
        await Insert("System", "users", new BsonDocument("_id", "a-user"));

        // What the kernel created in the configured location while it ran.
        await Insert("_system+es", "namespaces", new BsonDocument("_id", "Default"));
        await Insert("_system+es+Default", "event-log", new BsonDocument("_id", 0));
        await Insert("_system", "observer-states", new BsonDocument("_id", "running"));
    }

    async Task Because() => await _relocation.MoveTo(SystemEventStoreLocation.Configured);

    [Fact] async Task should_remove_every_legacy_database() => (await Databases()).ShouldEqual(["_system", "_system+es", "_system+es+Default"]);
    [Fact] async Task should_bring_over_what_only_the_legacy_database_had() => (await Collection("_system+es+Default", "event-log").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(2L);
    [Fact] async Task should_bring_over_the_legacy_event_store_state() => (await Collection("_system+es", "event-types").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(1L);
    [Fact] async Task should_keep_what_the_kernel_created() => (await Collection("_system", "observer-states").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(1L);
    [Fact] async Task should_bring_over_the_legacy_read_models() => (await Collection("_system", "users").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(1L);
}
