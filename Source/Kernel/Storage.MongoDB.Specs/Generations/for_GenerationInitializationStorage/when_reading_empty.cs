// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationInitializationStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reading_empty(ReplicaSetMongoDBFixture fixture) : given.an_initialization_storage(fixture)
{
    IReadOnlyList<GenerationInitializationReceipt> _receipts;
    IReadOnlyList<GenerationInitializationBlocker> _blockers;

    async Task Because()
    {
        _receipts = await _audit.GetReceipts();
        _blockers = await _audit.GetBlockers();
    }
    [Fact] void should_have_no_receipts() => _receipts.Count.ShouldEqual(0);
    [Fact] void should_have_no_blockers() => _blockers.Count.ShouldEqual(0);
}
