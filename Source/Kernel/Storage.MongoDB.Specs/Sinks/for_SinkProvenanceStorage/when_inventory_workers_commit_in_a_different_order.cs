// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkProvenanceStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_inventory_workers_commit_in_a_different_order(ReplicaSetMongoDBFixture fixture) : given.a_provenance_database(fixture)
{
    SinkProvenanceState _first;
    SinkProvenanceState _late;
    SinkProvenanceState _marked;
    bool _identity;

    async Task Because()
    {
        var alias = new SinkProvenanceStorage(_client.GetDatabase(_databaseName));
        _identity = (await _storage.ReadState()).DatabaseId == (await alias.ReadState()).DatabaseId;
        _first = await _storage.CompleteInventory(false);
        _late = await alias.CompleteInventory(true);
        await Task.WhenAll(_storage.MarkQuarantined(), alias.CompleteInventory(false));
        _marked = await new SinkProvenanceStorage(_database).ReadState();
    }

    [Fact] void should_persist_a_shared_identity() => _identity.ShouldBeTrue();
    [Fact] void should_publish_clean_completion() => (_first.IsComplete && !_first.Restricted).ShouldBeTrue();
    [Fact] void should_ignore_late_inventory_evidence_after_completion() => _late.Restricted.ShouldBeFalse();
    [Fact] void should_preserve_independent_explicit_marks() => _marked.Restricted.ShouldBeTrue();
}
