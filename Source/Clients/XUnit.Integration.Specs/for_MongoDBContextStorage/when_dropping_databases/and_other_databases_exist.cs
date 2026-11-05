// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBContextStorage.when_dropping_databases;

public class and_other_databases_exist : Specification
{
    MongoDBContextStorage _storage;
    IMongoClient _client;
    string _firstDatabase;
    string _secondDatabase;
    string _unissuedDatabase;
    IMongoDatabase _first;
    IMongoDatabase _second;
    string _firstProbe;
    string _secondProbe;

    void Establish()
    {
        _storage = new(_ => throw new Exception("No reader should be created"));
        _firstDatabase = $"{_storage.BeginContext()}{Constants.ReadModelsDatabaseName}";
        _secondDatabase = $"{_storage.BeginContext()}chronicle+main";
        _unissuedDatabase = "c10_unissued";
        var cursor = Substitute.For<IAsyncCursor<string>>();
        cursor.Current.Returns([_firstDatabase, _secondDatabase, _unissuedDatabase, "testing", "orleans", "admin", "config", "local"]);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        _client = Substitute.For<IMongoClient>();
        _client.ListDatabaseNamesAsync(Arg.Any<CancellationToken>()).Returns(cursor);
        _client.DropDatabaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _first = Substitute.For<IMongoDatabase>();
        _second = Substitute.For<IMongoDatabase>();
        _client.GetDatabase(_firstDatabase, Arg.Any<MongoDatabaseSettings>()).Returns(_first);
        _client.GetDatabase(_secondDatabase, Arg.Any<MongoDatabaseSettings>()).Returns(_second);
        _first.CreateCollectionAsync(Arg.Any<string>(), Arg.Any<CreateCollectionOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _firstProbe = call.ArgAt<string>(0);
            return Task.CompletedTask;
        });
        _second.CreateCollectionAsync(Arg.Any<string>(), Arg.Any<CreateCollectionOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _secondProbe = call.ArgAt<string>(0);
            return Task.CompletedTask;
        });
        _first.DropCollectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _second.DropCollectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    Task Because() => _storage.DropDatabases(_client);

    void Destroy() => _storage.ResetReaders();

    [Fact] void should_drop_the_previous_context_database() => _client.Received(1).DropDatabaseAsync(_firstDatabase, Arg.Any<CancellationToken>());
    [Fact] void should_drop_the_current_context_database() => _client.Received(1).DropDatabaseAsync(_secondDatabase, Arg.Any<CancellationToken>());
    [Fact] void should_drop_only_the_two_issued_databases() => _client.Received(2).DropDatabaseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_leave_an_unissued_prefix_untouched() => _client.DidNotReceive().DropDatabaseAsync(_unissuedDatabase, Arg.Any<CancellationToken>());
    [Fact] void should_leave_unprefixed_storage_untouched() => _client.DidNotReceive().DropDatabaseAsync("testing", Arg.Any<CancellationToken>());
    [Fact] void should_leave_system_databases_untouched() => _client.DidNotReceive().DropDatabaseAsync("admin", Arg.Any<CancellationToken>());
    [Fact] void should_probe_the_previous_context_database() => _first.Received(1).CreateCollectionAsync(Arg.Any<string>(), Arg.Any<CreateCollectionOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_probe_the_current_context_database() => _second.Received(1).CreateCollectionAsync(Arg.Any<string>(), Arg.Any<CreateCollectionOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_remove_the_previous_context_probe() => _first.Received(1).DropCollectionAsync(_firstProbe, Arg.Any<CancellationToken>());
    [Fact] void should_remove_the_current_context_probe() => _second.Received(1).DropCollectionAsync(_secondProbe, Arg.Any<CancellationToken>());
    [Fact] void should_resolve_only_issued_databases() => _client.Received(2).GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_not_resolve_unissued_databases() => _client.DidNotReceive().GetDatabase(_unissuedDatabase, Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_not_resolve_unprefixed_databases() => _client.DidNotReceive().GetDatabase("testing", Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_not_resolve_system_databases() => _client.DidNotReceive().GetDatabase(Arg.Is<string>(name => name == "admin" || name == "config" || name == "local"), Arg.Any<MongoDatabaseSettings>());
}
