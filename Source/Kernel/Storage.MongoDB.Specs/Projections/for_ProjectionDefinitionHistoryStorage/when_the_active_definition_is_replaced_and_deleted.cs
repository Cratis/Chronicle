// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionHistoryStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_the_active_definition_is_replaced_and_deleted(ReplicaSetMongoDBFixture fixture) : given.a_history_storage(fixture)
{
    ProjectionDefinition _latest;
    async Task Because()
    {
        await _storage.Record(_entry);
        await _active.Save(_entry.Definition);
        var changed = _entry with { Digest = new string('B', 64), Definition = _entry.Definition with { IsActive = false } };
        await _storage.Record(changed);
        await _active.Save(changed.Definition);
        _latest = await _active.Get(_entry.Projection);
        await _active.Delete(_entry.Projection);
        await Read();
    }
    [Fact] void should_read_the_latest_active_definition() => _latest.IsActive.ShouldBeFalse();
    [Fact] void should_retain_both_history_snapshots() => _entries.Length.ShouldEqual(2);
}
