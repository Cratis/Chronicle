// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

/// <summary>
/// A sink whose observed collection can be made to exist or not exist, read only when the spec says so.
/// </summary>
public class a_sink_observing_a_container : a_sink_with_indexes
{
    protected a_manually_read_change_streams _changeStreams;
    protected IMongoDatabase _database;
    protected bool _collectionExists;
    protected BsonDocument[] _documents = [];
    protected List<IEnumerable<ExpandoObject>> _pages = [];

    void Establish()
    {
        _changeStreams = new();
        _database = Substitute.For<IMongoDatabase>();
        _collection.Database.Returns(_database);
        _collections.GetCollection(Arg.Any<string>()).Returns(_collection);

        _database.ListCollectionNamesAsync(Arg.Any<ListCollectionNamesOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CursorOf<string>(_collectionExists ? [_readModel.ContainerName.Value] : [])));
        _collection.FindAsync(
                Arg.Any<FilterDefinition<BsonDocument>>(),
                Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(CursorOf(_collectionExists ? _documents : [])));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<BsonDocument>(), Arg.Any<JsonSchema>()).Returns(_ => new ExpandoObject());

        _sink = new Sink(_readModel, _converter, _collections, _changesetConverter, _expandoObjectConverter, _changeStreams);
    }

    static IAsyncCursor<T> CursorOf<T>(T[] items)
    {
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.Current.Returns(items);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(items.Length > 0, false);
        return cursor;
    }
}
