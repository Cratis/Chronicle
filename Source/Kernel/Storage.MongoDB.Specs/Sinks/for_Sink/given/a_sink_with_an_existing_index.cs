// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

public class a_sink_with_an_existing_index : a_sink_with_indexes
{
    protected BsonDocument _existingIndex;
    protected BsonDocument _collectionOptions;
    protected IAsyncCursor<BsonDocument> _collectionCursor;
    protected IMongoDatabase _database;

    void Establish()
    {
        _existingIndex = new BsonDocument
        {
            { "name", $"{_indexedProperty.Path}_1" },
            { "key", new BsonDocument(_indexedProperty.Path, 1) }
        };
        _indexCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        _indexCursor.Current.Returns([_existingIndex]);

        _collectionOptions = new BsonDocument();
        _collectionCursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        _collectionCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        _collectionCursor.Current.Returns([new BsonDocument("options", _collectionOptions)]);
        _database = Substitute.For<IMongoDatabase>();
        _collection.Database.Returns(_database);
        _collection.CollectionNamespace.Returns(new CollectionNamespace("readModels", "Something"));
        _database.ListCollectionsAsync(Arg.Any<ListCollectionsOptions>(), Arg.Any<CancellationToken>()).Returns(_collectionCursor);
    }
}
