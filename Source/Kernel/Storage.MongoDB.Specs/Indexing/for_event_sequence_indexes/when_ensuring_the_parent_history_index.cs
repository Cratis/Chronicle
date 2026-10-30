// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_event_sequence_indexes;

/// <summary>
/// Parent predecessor reads need a compound access path ending in sequence order.
/// </summary>
/// <param name="fixture">The shared MongoDB fixture.</param>
[Collection(MongoDBCollection.Name)]
public class when_ensuring_the_parent_history_index(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    IReadOnlyList<string> _indexes;
    BsonDocument _keys;

    async Task Because()
    {
        var collection = _database.GetEventSequenceCollectionFor(EventSequenceId.Log).CollectionNamespace.CollectionName;
        await _database.EnsureIndexesForEventSequence(EventSequenceId.Log);
        await _database.EnsureIndexesForEventSequence(EventSequenceId.Log);
        _indexes = await IndexNamesFor(collection);
        _keys = _indexes.Contains("eventSourceId_eventTypeId_sequenceNumber")
            ? (await IndexFor(collection, "eventSourceId_eventTypeId_sequenceNumber"))["key"].AsBsonDocument
            : [];
    }

    [Fact] void should_create_the_parent_history_index() => _indexes.ShouldContain("eventSourceId_eventTypeId_sequenceNumber");
    [Fact] void should_index_the_stored_source_type_and_sequence_fields() => string.Join(',', _keys.Names).ShouldEqual("eventSourceId,type,_id");
    [Fact] void should_order_predecessors_by_descending_sequence() => string.Join(',', _keys.Values.Select(value => value.ToInt32())).ShouldEqual("1,1,-1");
}
