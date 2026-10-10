// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_publishing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_appended_generation_is_recorded(ReplicaSetMongoDBFixture fixture) : given.a_storage_for_appended_generation(fixture)
{
    Event _stored;
    AppendedEvent _read;

    async Task Because()
    {
        await _storage.EnsureIndexes();
        (await _storage.AppendPublication(new EventPublication("publication", "intent"), GenerationalEvent(0, 1))).IsSuccess.ShouldBeTrue();
        _stored = await Stored(0);
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_record_the_original_generation() => _stored.AppendedGeneration.ShouldEqual((uint?)1);
    [Fact] void should_still_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_still_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
