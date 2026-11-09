// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation.when_finding_retained_legacy_data;

/// <summary>
/// Before the move the legacy databases hold the only copy, which is misplaced data rather than a stale leftover.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_move_has_not_happened(MongoDBFixture fixture) : given.a_system_event_store_relocation(fixture)
{
    string? _result;

    async Task Establish()
    {
        await Insert("System+es", "event-types", new BsonDocument("_id", "only-copy"));
        await Insert("System+es+Default", "event-log", new BsonDocument("_id", 0));
    }

    async Task Because() => _result = await _relocation.FindRetainedLegacyData();

    [Fact] void should_find_nothing_retained() => _result.ShouldBeNull();
}
