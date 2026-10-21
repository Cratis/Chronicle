// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBContextStorage.when_beginning_a_context;

public class and_readers_were_cached : Specification
{
    MongoDBContextStorage _storage;
    MongoDBDatabase _previousEventStore;
    MongoDBDatabase _previousNamespace;
    MongoDBDatabase _previousReadModels;
    string _prefix;

    void Establish()
    {
        _storage = new(CreateDatabase);
        _storage.BeginContext();
        _previousEventStore = _storage.EventStore;
        _previousNamespace = _storage.EventStoreForNamespace;
        _previousReadModels = _storage.ReadModels;
    }

    void Because() => _prefix = _storage.BeginContext();

    void Destroy() => _storage.ResetReaders();

    static MongoDBDatabase CreateDatabase(string name)
    {
        var database = Substitute.For<IMongoDatabase>();
        database.DatabaseNamespace.Returns(new DatabaseNamespace(name));
        return new(database);
    }

    [Fact] void should_replace_the_event_store_reader() => _storage.EventStore.ShouldNotBeSame(_previousEventStore);
    [Fact] void should_replace_the_namespace_reader() => _storage.EventStoreForNamespace.ShouldNotBeSame(_previousNamespace);
    [Fact] void should_replace_the_read_model_reader() => _storage.ReadModels.ShouldNotBeSame(_previousReadModels);
    [Fact] void should_read_the_current_event_store() => _storage.EventStore.Database.DatabaseNamespace.DatabaseName.ShouldEqual($"{_prefix}{Constants.EventStoreDatabaseName}");
    [Fact] void should_read_the_current_namespace() => _storage.EventStoreForNamespace.Database.DatabaseNamespace.DatabaseName.ShouldEqual($"{_prefix}{Constants.EventStoreNamespaceDatabaseName}");
    [Fact] void should_read_the_current_read_models() => _storage.ReadModels.Database.DatabaseNamespace.DatabaseName.ShouldEqual($"{_prefix}{Constants.ReadModelsDatabaseName}");
}
