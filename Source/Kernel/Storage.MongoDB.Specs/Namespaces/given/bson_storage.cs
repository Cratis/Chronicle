// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using Cratis.Chronicle.Concepts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.given;

public class bson_storage : Specification
{
    protected static readonly EventStoreNamespaceName _name = "Tenant";
    protected readonly NamespaceIncarnationId _identity = NamespaceIncarnationId.New();
    protected IMongoCollection<BsonDocument> _collection;
    protected IClientSessionHandle _session;
    protected IClientSessionHandle _usedSession;
    protected BsonDocument _document;
    protected BsonDocument _inserted;
    protected BsonDocument _replacement;
    protected BsonDocument _filter;
    protected BsonDocument _update;
    protected bool _upsert;
    protected long _matches = 1;
    protected long _modified = 1;
    protected int _reads;
    protected Exception _insertFailure;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _collection.WithWriteConcern(WriteConcern.WMajority).Returns(_collection);
        _collection.WithReadConcern(ReadConcern.Majority).Returns(_collection);
        _collection.WithReadPreference(ReadPreference.Primary).Returns(_collection);
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), default).Returns(_ =>
        {
            _reads++;
            var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
            IEnumerable<BsonDocument> rows = _document is null ? [] : [_document];
            cursor.Current.Returns(rows);
            cursor.MoveNextAsync(default).Returns(Task.FromResult(true), Task.FromResult(false));
            return Task.FromResult(cursor);
        });
        _collection.InsertOneAsync(Arg.Any<BsonDocument>(), default, default).Returns(call =>
        {
            if (_insertFailure is not null) throw _insertFailure;
            _inserted = call.Arg<BsonDocument>();
            return Task.CompletedTask;
        });
        _collection.ReplaceOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<BsonDocument>(), Arg.Any<ReplaceOptions>(), default).Returns(call =>
        {
            _filter = Render(call.Arg<FilterDefinition<BsonDocument>>());
            _replacement = call.Arg<BsonDocument>();
            _upsert = call.Arg<ReplaceOptions>().IsUpsert;
            return Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(_matches, _modified, null));
        });
        _collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), default).Returns(call => RecordUpdate(call));
        _collection.UpdateOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), default).Returns(call =>
        {
            _usedSession = call.Arg<IClientSessionHandle>();
            return RecordUpdate(call);
        });
        _session = Substitute.For<IClientSessionHandle>();
        _session.IsInTransaction.Returns(true);
    }

    protected static BsonDocument Marker(NamespaceIncarnationId identity, bool open, long revision = 0) => new()
    {
        { "_id", 1 }, { "incarnation", new BsonBinaryData(identity.Value, GuidRepresentation.Standard) }, { "isOpen", open }, { "revision", revision }
    };

    protected static MongoWriteException DuplicateKey()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var error = (WriteError)Activator.CreateInstance(typeof(WriteError), flags, null, [ServerErrorCategory.DuplicateKey, 11000, "Duplicate key", new BsonDocument()], null)!;
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        return new(connection, error, null, null);
    }

    static BsonDocument Render(FilterDefinition<BsonDocument> filter) => filter.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));

    Task<UpdateResult> RecordUpdate(CallInfo call)
    {
        _filter = Render(call.Arg<FilterDefinition<BsonDocument>>());
        _update = call.Arg<UpdateDefinition<BsonDocument>>().Render(new(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
        _upsert = call.Arg<UpdateOptions>().IsUpsert;
        return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(_matches, _modified, null));
    }
}
