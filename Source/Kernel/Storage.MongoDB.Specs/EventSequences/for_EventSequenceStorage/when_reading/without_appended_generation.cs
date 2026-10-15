// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_reading;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class without_appended_generation(ReplicaSetMongoDBFixture fixture) : given.a_storage_for_appended_generation(fixture)
{
    Event _stored;
    AppendedEvent _read;

    async Task Establish()
    {
        (await _storage.AppendMany([GenerationalEvent(0, 1)])).IsSuccess.ShouldBeTrue();
        var document = (await Stored(0)).ToBsonDocument();
        var memberName = global::MongoDB.Bson.Serialization.BsonClassMap.LookupClassMap(typeof(Event)).GetMemberMap(nameof(Event.AppendedGeneration)).ElementName;
        document.Remove(memberName);
        document.Contains(memberName).ShouldBeFalse();
        await _rawEvents.ReplaceOneAsync(FilterDefinition<BsonDocument>.Empty, document);
    }

    async Task Because()
    {
        _stored = await Stored(0);
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_leave_the_original_generation_unknown() => _stored.AppendedGeneration.ShouldBeNull();
    [Fact] void should_still_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_still_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
