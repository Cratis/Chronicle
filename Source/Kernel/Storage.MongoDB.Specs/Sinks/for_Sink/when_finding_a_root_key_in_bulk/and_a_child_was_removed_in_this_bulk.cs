// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_root_key_in_bulk.given;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_root_key_in_bulk;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_child_was_removed_in_this_bulk(ReplicaSetMongoDBFixture fixture) : a_bulk_sink_with_children(fixture)
{
    Option<Key> _beforeRemoval;
    Option<Key> _afterRemoval;

    async Task Because()
    {
        var child = Child("X");
        await _sink.ApplyChanges(_key, ChangesWithState(RootWith("X"), new ChildAdded(child, "children", "childId", "X", IndexerFor("X"))), 1UL);
        _beforeRemoval = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), "X");
        await _sink.ApplyChanges(_key, ChangesWithState(RootWith(), new ChildRemoved(child, "children", "childId", "X")), 2UL);
        _afterRemoval = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), "X");
    }

    [Fact] void should_resolve_the_child_before_it_is_removed() => _beforeRemoval.TryGetValue(out _).ShouldBeTrue();
    [Fact] void should_not_resolve_the_removed_child() => _afterRemoval.TryGetValue(out _).ShouldBeFalse();
}
