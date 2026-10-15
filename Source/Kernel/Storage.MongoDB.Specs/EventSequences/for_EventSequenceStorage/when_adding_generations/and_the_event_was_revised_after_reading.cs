// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_adding_generations;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_event_was_revised_after_reading(ReplicaSetMongoDBFixture fixture) : given.a_storage_with_stored_generations(fixture)
{
    string _before;
    string _after;
    bool _added;

    async Task Establish()
    {
        await Revise();
        _before = await Fingerprint();
    }

    async Task Because()
    {
        _added = await _storage.TryAddGenerations(_observed, [Target()]);
        _after = await Fingerprint();
    }

    [Fact] void should_reject_the_write() => _added.ShouldBeFalse();
    [Fact] void should_preserve_content_hashes_and_provenance() => _after.ShouldEqual(_before);
}
