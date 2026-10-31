// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_stored_generations;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_appended_generation_is_unknown(ReplicaSetMongoDBFixture fixture) : given.a_storage_with_stored_generations(fixture)
{
    StoredEventGenerations _snapshot;

    async Task Establish()
    {
        await ClearAppendedGeneration();
    }

    async Task Because()
    {
        _snapshot = (await _storage.GetStoredGenerations(0))!;
    }

    [Fact] void should_not_infer_the_appended_generation() => _snapshot.AppendedGeneration.ShouldBeNull();
    [Fact] void should_keep_the_stored_generations() => _snapshot.Content.Count.ShouldEqual(2);
}
