// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_keys_holding_a_child;

public class and_a_cached_parent_is_also_persisted : given.a_bulk_child_removal_from_all
{
    IReadOnlyCollection<Key> _keys;

    async Task Establish()
    {
        // The cached write key carries the child indexers of the event that wrote it.
        var indexers = new ArrayIndexers([new ArrayIndexer("[items]", "childId", "item")]);
        await Cache(new Key("parent", indexers), Document("items", new List<System.Dynamic.ExpandoObject> { Document("childId", "item") }));

        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        cursor.Current.Returns([new BsonDocument("_id", "parent")]);
        _collection.FindAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));
    }

    async Task Because() => _keys = await _sink.FindKeysHoldingChild(new ChildRemovedFromAll("[items]", "childId", "item", ArrayIndexers.NoIndexers));

    [Fact] void should_return_the_parent_once() => _keys.Count.ShouldEqual(1);
    [Fact] void should_return_the_root_key() => _keys.Single().ArrayIndexers.ShouldEqual(ArrayIndexers.NoIndexers);
}
