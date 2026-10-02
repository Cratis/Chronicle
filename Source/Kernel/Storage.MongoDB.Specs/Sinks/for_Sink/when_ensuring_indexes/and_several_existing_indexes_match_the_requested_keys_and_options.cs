// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ensuring_indexes;

public class and_several_existing_indexes_match_the_requested_keys_and_options : given.a_sink_with_an_existing_index
{
    void Establish()
    {
        _readModel = _readModel with { Indexes = [new IndexDefinition(_indexedProperty), new IndexDefinition("OtherProperty")] };
        _sink = new Sink(_readModel, _converter, _collections, _changesetConverter, _expandoObjectConverter, Substitute.For<IReadModelChangeStreams>());
        _indexCursor.Current.Returns(
        [
            _existingIndex,
            new BsonDocument { { "name", "OtherProperty_1" }, { "key", new BsonDocument("OtherProperty", 1) } }
        ]);
    }

    async Task Because() => await _sink.EnsureIndexes();

    [Fact] void should_fetch_the_collection_collation_only_once() =>
        _database.Received(1).ListCollectionsAsync(Arg.Any<ListCollectionsOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_create_any_indexes() =>
        _indexManager.DidNotReceive().CreateOneAsync(
            Arg.Any<CreateIndexModel<BsonDocument>>(),
            Arg.Any<CreateOneIndexOptions>(),
            Arg.Any<CancellationToken>());
}
