// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationInitializationStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_receipts_race(ReplicaSetMongoDBFixture fixture) : given.an_initialization_storage(fixture)
{
    bool[] _results;

    async Task Because() => _results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => _audit.TryRecord(Receipt()))));
    [Fact] void should_have_one_winner() => _results.Count(_ => _).ShouldEqual(1);
    [Fact] async Task should_have_one_receipt() => (await _audit.GetReceipts()).Count.ShouldEqual(1);
}
