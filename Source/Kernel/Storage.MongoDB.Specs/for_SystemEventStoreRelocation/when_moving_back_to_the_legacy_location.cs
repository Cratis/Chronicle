// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation;

[Collection(MongoDBCollection.Name)]
public class when_moving_back_to_the_legacy_location(MongoDBFixture fixture) : given.a_system_event_store_relocation(fixture)
{
    async Task Establish()
    {
        await Insert("_system+es", "namespaces", new BsonDocument("_id", "Default"));
        await Insert("_system+es+Default", "event-log", new BsonDocument("_id", 0));
        await Insert("_system", "users", new BsonDocument("_id", "a-user"));
    }

    async Task Because() => await _relocation.MoveTo(SystemEventStoreLocation.Legacy);

    [Fact] async Task should_leave_only_the_legacy_databases() => (await Databases()).ShouldEqual(["System", "System+es", "System+es+Default"]);
    [Fact] async Task should_move_the_event_log() => (await Collection("System+es+Default", "event-log").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(1L);
}
