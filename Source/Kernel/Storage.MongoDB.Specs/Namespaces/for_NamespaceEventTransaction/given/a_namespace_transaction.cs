// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceEventTransaction.given;

public class a_namespace_transaction : Specification
{
    protected readonly NamespaceIncarnationId _identity = NamespaceIncarnationId.New();
    protected readonly CancellationTokenSource _activation = new();
    protected readonly List<IClientSessionHandle> _sessions = [];
    protected readonly List<IClientSessionHandle> _writeSessions = [];
    protected readonly List<string> _steps = [];
    protected IEventStoreNamespaceDatabase _database;
    protected IMongoDatabase _destination;
    protected IMongoCollection<BsonDocument> _markers;
    protected NamespaceCommitContext _context;
    protected TransactionOptions _options;
    protected CancellationToken _fenceCancellation;
    protected CancellationToken _writeCancellation;
    protected CancellationToken _commitCancellation;
    protected Exception _fenceFailure;
    protected Exception _writeFailure;
    protected Exception _commitFailure;
    protected Exception _failure;
    protected Action _duringWrite;
    protected Action _onFenceFailure;
    protected int _fenceFailures;
    protected int _commitFailures;
    protected int _attempts;
    protected int _commits;
    protected int _aborts;
    protected int _result;
    protected long _matches = 1;
    NamespaceEventTransaction _transaction;

    protected static MongoCommandException CommandFailure(int code, string? label = null)
    {
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        var error = new MongoCommandException(connection, "Command failed", new BsonDocument("insert", "events"), new BsonDocument("code", code));
        if (label is not null) error.AddErrorLabel(label);
        return error;
    }

    void Establish()
    {
        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        var client = Substitute.For<IMongoClient>();
        _database.Client.Returns(client);
        _destination = Substitute.For<IMongoDatabase>();
        _destination.Client.Returns(client);
        _destination.DatabaseNamespace.Returns(new DatabaseNamespace($"namespace-{Guid.NewGuid():N}"));
        _destination.RunCommandAsync(Arg.Any<Command<BsonDocument>>(), Arg.Any<ReadPreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new BsonDocument("setName", "rs0")));
        _markers = Substitute.For<IMongoCollection<BsonDocument>>();
        _markers.Database.Returns(_destination);
        _markers.WithWriteConcern(WriteConcern.WMajority).Returns(_markers);
        _markers.WithReadConcern(ReadConcern.Majority).Returns(_markers);
        _markers.WithReadPreference(ReadPreference.Primary).Returns(_markers);
        _database.GetCollection<BsonDocument>(WellKnownCollectionNames.NamespaceMarker).Returns(_markers);
        _markers.UpdateOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _steps.Add("fence");
                _fenceCancellation = call.Arg<CancellationToken>();
                if (_attempts <= _fenceFailures)
                {
                    _onFenceFailure?.Invoke();
                    throw _fenceFailure;
                }
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(_matches, _matches, null));
            });
        client.StartSessionAsync(Arg.Any<ClientSessionOptions>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(StartSession()));
        _transaction = new(_database);
        _context = new(_identity, _activation.Token) { ActivationIncarnation = _identity };
    }

    protected async Task Execute() => _result = await _transaction.Execute(_context, Write);

    protected async Task ExecuteCatching() => _failure = await Catch.Exception(Execute);

    IClientSessionHandle StartSession()
    {
        _attempts++;
        var session = Substitute.For<IClientSessionHandle>();
        _sessions.Add(session);
        session.IsInTransaction.Returns(true);
        session.When(value => value.StartTransaction(Arg.Any<TransactionOptions>())).Do(call => _options = call.Arg<TransactionOptions>());
        session.CommitTransactionAsync(Arg.Any<CancellationToken>()).Returns(call =>
        {
            _steps.Add("commit");
            _commits++;
            _commitCancellation = call.Arg<CancellationToken>();
            if (_commits <= _commitFailures) throw _commitFailure;
            return Task.CompletedTask;
        });
        session.AbortTransactionAsync(CancellationToken.None).Returns(_ =>
        {
            _steps.Add("abort");
            _aborts++;
            return Task.CompletedTask;
        });
        return session;
    }

    Task<int> Write(IClientSessionHandle session, CancellationToken cancellationToken)
    {
        _steps.Add("write");
        _writeSessions.Add(session);
        _writeCancellation = cancellationToken;
        _duringWrite?.Invoke();
        if (_writeFailure is not null) throw _writeFailure;
        return Task.FromResult(42);
    }

    void Destroy() => _activation.Dispose();
}
