// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_recording_concurrently(ReplicaSetMongoDBFixture fixture) : given.a_history_storage(fixture)
{
    async Task Because()
    {
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => _storage.Record(_entry)));
        await Read();
    }
    [Fact] void should_keep_one_entry() => _entries.Length.ShouldEqual(1);
}
