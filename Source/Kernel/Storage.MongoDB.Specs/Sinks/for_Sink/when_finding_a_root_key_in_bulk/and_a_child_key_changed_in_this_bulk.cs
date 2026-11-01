// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_root_key_in_bulk.given;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_root_key_in_bulk;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_a_child_key_changed_in_this_bulk(ReplicaSetMongoDBFixture fixture) : a_bulk_sink_with_children(fixture)
{
    Option<Key> _oldKey;
    Option<Key> _newKey;

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesWithState(RootWith("A"), new ChildAdded(Child("A"), "children", "childId", "A", IndexerFor("A"))), 1UL);
        await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), "A");
        var change = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath("children[childId].childId"), "A", "B", IndexerFor("A"))]);
        await _sink.ApplyChanges(_key, ChangesWithState(RootWith("B"), change), 2UL);
        _oldKey = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), "A");
        _newKey = await _sink.TryFindRootKeyByChildValue(new PropertyPath("children.childId"), "B");
    }

    [Fact] void should_not_resolve_the_old_child_key() => _oldKey.TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_resolve_the_new_child_key() => _newKey.TryGetValue(out _).ShouldBeTrue();
}
