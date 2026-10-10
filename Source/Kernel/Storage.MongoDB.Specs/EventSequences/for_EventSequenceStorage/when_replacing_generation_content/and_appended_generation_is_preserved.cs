// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_replacing_generation_content;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_appended_generation_is_preserved(ReplicaSetMongoDBFixture fixture) : given.a_storage_for_appended_generation(fixture)
{
    Event _stored;
    AppendedEvent _read;

    async Task Establish() => (await _storage.AppendMany([GenerationalEvent(0, 1)])).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        await _storage.ReplaceGenerationContent(0, Content());
        _stored = await Stored(0);
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_keep_the_original_generation() => _stored.AppendedGeneration.ShouldEqual((uint?)1);
    [Fact] void should_still_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_still_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
