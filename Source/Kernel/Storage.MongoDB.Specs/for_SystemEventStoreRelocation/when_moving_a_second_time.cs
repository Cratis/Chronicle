// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation;

/// <summary>
/// Moving again, as the repeat patch does on an installation where the first move worked, must change nothing.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_moving_a_second_time(MongoDBFixture fixture) : given.a_system_event_store_relocation(fixture)
{
    async Task Establish()
    {
        await Insert("System+es", "namespaces", new BsonDocument("_id", "Default"));
        await Insert("System+es+Default", "event-log", new BsonDocument("_id", 0), new BsonDocument("_id", 1));
        await _relocation.MoveTo(SystemEventStoreLocation.Configured);
    }

    async Task Because() => await _relocation.MoveTo(SystemEventStoreLocation.Configured);

    [Fact] async Task should_still_have_only_the_configured_databases() => (await Databases()).ShouldEqual(["_system+es", "_system+es+Default"]);
    [Fact] async Task should_keep_every_document() => (await Collection("_system+es+Default", "event-log").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(2L);
}
