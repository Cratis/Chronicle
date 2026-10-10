// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_appended_generations_are_mixed(ReplicaSetMongoDBFixture fixture) : given.a_storage_for_appended_generation(fixture)
{
    Event _first;
    Event _second;
    AppendedEvent _read;

    async Task Because()
    {
        (await _storage.AppendMany([GenerationalEvent(0, 1), GenerationalEvent(1, 2)])).IsSuccess.ShouldBeTrue();
        _first = await Stored(0);
        _second = await Stored(1);
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_record_the_first_events_appended_generation() => _first.AppendedGeneration.ShouldEqual((uint?)1);
    [Fact] void should_record_the_second_events_appended_generation() => _second.AppendedGeneration.ShouldEqual((uint?)2);
    [Fact] void should_still_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_still_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
