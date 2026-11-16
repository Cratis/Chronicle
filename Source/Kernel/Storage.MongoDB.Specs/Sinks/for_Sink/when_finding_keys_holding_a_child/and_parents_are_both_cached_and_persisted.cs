// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_keys_holding_a_child;

public class and_parents_are_both_cached_and_persisted : given.a_bulk_child_removal_from_all
{
    readonly Key _stringKey = new("parent", ArrayIndexers.NoIndexers);
    readonly Key _guidKey = new(Guid.Parse("8563a013-15d6-405a-9d33-71479edf96a0"), ArrayIndexers.NoIndexers);
    IReadOnlyCollection<Key> _result;

    async Task Establish()
    {
        await Cache(_stringKey, Document("items", new[] { Document("childId", "item") }));
        await Cache(_guidKey, Document("items", new[] { Document("childId", "item") }));
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        cursor.Current.Returns([
            new BsonDocument("_id", (string)_stringKey.Value),
            new BsonDocument("_id", new BsonBinaryData((Guid)_guidKey.Value, GuidRepresentation.Standard))
        ]);
        _collection.FindAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
            Arg.Any<CancellationToken>()).Returns(cursor);
    }

    async Task Because() => _result = await _sink.FindKeysHoldingChild(new ChildRemovedFromAll("[items]", "childId", "item", ArrayIndexers.NoIndexers));

    [Fact] void should_return_each_parent_only_once() => _result.Count.ShouldEqual(2);
    [Fact] void should_return_the_string_parent() => _result.ShouldContain(_stringKey);
    [Fact] void should_return_the_guid_parent() => _result.ShouldContain(_guidKey);
}
