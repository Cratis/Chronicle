// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceStorage.given;

public class a_namespace_database : Specification
{
    protected EventStoreNamespaceDatabase _database;
    protected IMongoDatabase _mongo;
    protected readonly List<string> _names = [];
    protected readonly HashSet<string> _nonEmpty = [];
    protected readonly List<string> _probes = [];
    protected readonly List<FindOptions<BsonDocument, BsonDocument>> _findOptions = [];
    protected readonly List<IDisposable> _cursors = [];
    protected Exception? _readFailure;

    void Establish()
    {
        _mongo = Substitute.For<IMongoDatabase>();
        var connections = Substitute.For<IMongoDBStorageConnections>();
        connections.GetDatabase(StorageConnectionKind.Namespace, Arg.Any<string>(), "store", "namespace").Returns(_mongo);
        _database = new("store", "namespace", Substitute.For<IMongoDBClientManager>(), Options.Create(new MongoDBOptions()), connections: connections);
        _mongo.ListCollectionNamesAsync(Arg.Any<ListCollectionNamesOptions>(), default).Returns(_ => Cursor(_names));
        _mongo.GetCollection<BsonDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(call => Collection(call.Arg<string>()));
    }

    IMongoCollection<BsonDocument> Collection(string name)
    {
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), default).Returns(call =>
        {
            _probes.Add(name);
            _findOptions.Add(call.Arg<FindOptions<BsonDocument, BsonDocument>>());
            return _readFailure is not null
                ? Task.FromException<IAsyncCursor<BsonDocument>>(_readFailure)
                : Task.FromResult(Cursor<BsonDocument>(_nonEmpty.Contains(name) ? [new BsonDocument("_id", 1)] : []));
        });
        return collection;
    }

    IAsyncCursor<T> Cursor<T>(IEnumerable<T> items)
    {
        var cursor = Substitute.For<IAsyncCursor<T>>();
        _cursors.Add(cursor);
        cursor.Current.Returns(items);
        cursor.MoveNextAsync(default).Returns(true, false);
        return cursor;
    }
}
