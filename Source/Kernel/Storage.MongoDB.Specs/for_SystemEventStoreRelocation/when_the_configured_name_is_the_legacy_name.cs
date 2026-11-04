// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation;

/// <summary>
/// A deployment can keep the old names by configuring them, and then nothing is moved.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_the_configured_name_is_the_legacy_name(MongoDBFixture fixture) : given.a_system_event_store_relocation(fixture)
{
    protected override string SystemDatabaseName => "System";

    async Task Establish() => await Insert("System+es+Default", "event-log", new BsonDocument("_id", 0));

    async Task Because() => await _relocation.MoveTo(SystemEventStoreLocation.Configured);

    [Fact] async Task should_leave_the_databases_where_they_are() => (await Databases()).ShouldEqual(["System+es+Default"]);
}
