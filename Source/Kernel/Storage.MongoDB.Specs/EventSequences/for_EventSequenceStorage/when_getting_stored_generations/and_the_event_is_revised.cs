// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_stored_generations;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_event_is_revised(ReplicaSetMongoDBFixture fixture) : given.a_storage_with_stored_generations(fixture)
{
    StoredEventGenerations _snapshot;

    async Task Establish()
    {
        await Revise();
    }

    async Task Because()
    {
        _snapshot = (await _storage.GetStoredGenerations(0))!;
    }

    [Fact] void should_read_the_base_content() => _snapshot.Content[EventTypeGeneration.First].ShouldEqual(_observed.Content[EventTypeGeneration.First]);
    [Fact] void should_report_the_revision_count() => _snapshot.RevisionCount.ShouldEqual(1);
}
