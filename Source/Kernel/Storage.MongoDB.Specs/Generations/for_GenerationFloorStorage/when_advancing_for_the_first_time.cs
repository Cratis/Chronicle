// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationFloorStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_advancing_for_the_first_time(ReplicaSetMongoDBFixture fixture) : given.a_floor_storage(fixture)
{
    GenerationFloorAdvanceResult _result;
    GenerationFloor _persisted;
    IReadOnlyList<GenerationFloor> _history;
    async Task Because()
    {
        _result = await _storage.Advance(Request());
        _persisted = await _storage.Get();
        _history = await _storage.GetHistory();
    }
    [Fact] void should_advance() => _result.Value.ShouldBeOfExactType<GenerationFloorAdvanced>();
    [Fact] void should_assign_revision_one() => _persisted.Revision.ShouldEqual(1UL);
    [Fact] void should_commit_matching_audit_entry() => _history.Single().Revision.ShouldEqual(_persisted.Revision);
    [Fact] void should_keep_requester() => _persisted.AdvancedBy.ShouldEqual("spec");
    [Fact] void should_keep_reason() => _persisted.Reason.ShouldEqual("explicit request");
}
