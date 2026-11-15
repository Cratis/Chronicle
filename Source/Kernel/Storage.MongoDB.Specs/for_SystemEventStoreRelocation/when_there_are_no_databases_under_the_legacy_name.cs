// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation;

/// <summary>
/// A fresh installation, or a hosted server that owns the legacy name, has nothing of ours to move.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_there_are_no_databases_under_the_legacy_name(MongoDBFixture fixture) : given.a_system_event_store_relocation(fixture)
{
    async Task Establish() => await Insert("_system+es+Default", "event-log", new BsonDocument("_id", 0));

    async Task Because() => await _relocation.MoveTo(SystemEventStoreLocation.Configured);

    [Fact] async Task should_leave_the_configured_databases_where_they_are() => (await Databases()).ShouldEqual(["_system+es+Default"]);
}
