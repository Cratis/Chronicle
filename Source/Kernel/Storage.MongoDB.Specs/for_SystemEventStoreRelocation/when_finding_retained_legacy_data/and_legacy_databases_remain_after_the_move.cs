// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation.when_finding_retained_legacy_data;

/// <summary>
/// A user that may not drop the legacy databases leaves them behind once their content has moved, and an operator
/// must be told which ones to remove.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_legacy_databases_remain_after_the_move(MongoDBFixture fixture) : given.a_system_event_store_relocation(fixture)
{
    string? _result;

    async Task Establish()
    {
        await Insert("_system+es+Default", "event-log", new BsonDocument("_id", 0));
        await Insert("System+es", "event-types", new BsonDocument("_id", "stale"));
        await Insert("System+es+Default", "event-log", new BsonDocument("_id", 0));
        await Insert("System", "users", new BsonDocument("_id", "a-user"));
        await Insert("SystemReports+es", "event-types", new BsonDocument("_id", "another-store"));
    }

    async Task Because() => _result = await _relocation.FindRetainedLegacyData();

    [Fact] void should_name_the_legacy_read_model_database() => _result!.ShouldContain($": '{_prefix}System', ");
    [Fact] void should_name_the_legacy_event_store_database() => _result!.ShouldContain($"'{_prefix}System+es'");
    [Fact] void should_name_the_legacy_namespace_database() => _result!.ShouldContain($"'{_prefix}System+es+Default'");
    [Fact] void should_not_name_another_event_store() => _result!.ShouldNotContain("SystemReports");
}
