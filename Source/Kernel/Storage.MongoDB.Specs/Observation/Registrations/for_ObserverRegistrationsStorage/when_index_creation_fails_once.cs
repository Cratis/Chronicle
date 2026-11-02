// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Registrations.for_ObserverRegistrationsStorage;

public class when_index_creation_fails_once : Specification
{
    ObserverRegistrationsStorage _storage;
    ObserverRegistration _registration;
    IMongoCollection<BsonDocument> _collection;
    IMongoIndexManager<BsonDocument> _indexes;
    TimeoutException _failure;
    Exception _error;
    bool _retriedSave;
    bool _subsequentSave;

    void Establish()
    {
        var database = Substitute.For<IEventStoreDatabase>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _indexes = Substitute.For<IMongoIndexManager<BsonDocument>>();
        database.GetCollection<BsonDocument>(Arg.Any<string>()).Returns(_collection);
        _collection.Indexes.Returns(_indexes);
        _failure = new TimeoutException("MongoDB was temporarily unavailable");
        _indexes.CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(_failure), Task.FromResult("index"));
        _storage = new ObserverRegistrationsStorage(database, new JsonSerializerOptions { Converters = { new ConceptAsJsonConverterFactory() } });
        _registration = new("observer", "event-log", ObserverType.Reactor, new ReactorRegistrationDefinition(new("observer", ReactorOwner.Client, "event-log", [], false, [], Concepts.Observation.ObserverFilters.None)))
        {
            Revision = new(1)
        };
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _storage.TrySave(_registration, new(0)));
        _retriedSave = await _storage.TrySave(_registration, new(0));
        _subsequentSave = await _storage.TrySave(_registration with { ObserverId = "another" }, new(0));
    }

    [Fact] void should_propagate_the_initial_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_retry_and_allow_the_next_save() => _retriedSave.ShouldBeTrue();
    [Fact] void should_allow_subsequent_saves() => _subsequentSave.ShouldBeTrue();
    [Fact] void should_cache_successful_index_creation() => _indexes.Received(2).CreateOneAsync(Arg.Any<CreateIndexModel<BsonDocument>>(), Arg.Any<CreateOneIndexOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_only_write_after_indexes_are_created() => _collection.Received(2).InsertOneAsync(Arg.Any<BsonDocument>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
}
