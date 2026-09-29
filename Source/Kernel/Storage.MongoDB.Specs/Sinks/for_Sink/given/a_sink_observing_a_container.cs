// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

/// <summary>
/// A sink whose observed collections - the primary and the promoting one - can be made to exist or not exist, read
/// only when the spec says so.
/// </summary>
public class a_sink_observing_a_container : a_sink_with_indexes
{
    protected a_manually_read_change_streams _changeStreams;
    protected IMongoDatabase _database;
    protected Guid? _primaryId;
    protected bool _promotingExists;
    protected Action? _duringFind;
    protected int _listCollectionsCalls;
    protected BsonDocument[] _documents = [];
    protected List<IEnumerable<ExpandoObject>> _pages = [];

    void Establish()
    {
        _changeStreams = new();
        _database = Substitute.For<IMongoDatabase>();
        _collection.Database.Returns(_database);
        _collections.GetCollection(Arg.Any<string>()).Returns(_collection);

        _collections.PromotingCollectionName.Returns(PromotingName);
        _database.ListCollectionsAsync(Arg.Any<ListCollectionsOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _listCollectionsCalls++;
                return Task.FromResult(CursorOf(ListedCollections()));
            });
        _collection.FindAsync(
                Arg.Any<FilterDefinition<BsonDocument>>(),
                Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _duringFind?.Invoke();
                return Task.FromResult(CursorOf(_primaryId is null ? [] : _documents));
            });
        _expandoObjectConverter.ToExpandoObject(Arg.Any<BsonDocument>(), Arg.Any<JsonSchema>()).Returns(_ => new ExpandoObject());

        _sink = new Sink(_readModel, _converter, _collections, _changesetConverter, _expandoObjectConverter, _changeStreams);
    }

    protected string PromotingName => $"replay-{_readModel.ContainerName}-promoting";

    BsonDocument[] ListedCollections()
    {
        var listed = new List<BsonDocument>();
        if (_primaryId is not null)
        {
            listed.Add(new BsonDocument
            {
                { "name", _readModel.ContainerName.Value },
                { "info", new BsonDocument("uuid", new BsonBinaryData(_primaryId.Value, GuidRepresentation.Standard)) }
            });
        }

        if (_promotingExists)
        {
            listed.Add(new BsonDocument("name", PromotingName));
        }

        return [.. listed];
    }

    static IAsyncCursor<T> CursorOf<T>(T[] items)
    {
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.Current.Returns(items);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(items.Length > 0, false);
        return cursor;
    }
}
