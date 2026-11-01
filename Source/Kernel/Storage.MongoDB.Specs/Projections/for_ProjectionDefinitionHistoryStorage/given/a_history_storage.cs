// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Projections;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionDefinitionHistoryStorage.given;

public class a_history_storage(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected ProjectionDefinitionHistoryStorage _storage;
    protected ProjectionDefinitionsStorage _active;
    protected ProjectionDefinitionHistoryEntry _entry;
    protected ProjectionDefinitionHistoryEntry[] _entries;
    protected IMongoDatabase _database;
    MongoClient _client;
    string _databaseName;

    void Establish()
    {
        _databaseName = MongoDBSpecDatabaseNames.ForScenario();
        _client = new MongoClient(fixture.ConnectionString);
        _database = _client.GetDatabase(_databaseName);
        var eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        eventStoreDatabase.GetCollection<ProjectionDefinitionHistoryDocument>(WellKnownCollectionNames.ProjectionDefinitionHistory)
            .Returns(_database.GetCollection<ProjectionDefinitionHistoryDocument>(WellKnownCollectionNames.ProjectionDefinitionHistory));
        eventStoreDatabase.GetCollection<Projection>(WellKnownCollectionNames.ProjectionDefinitions)
            .Returns(_database.GetCollection<Projection>(WellKnownCollectionNames.ProjectionDefinitions));
        _storage = new(eventStoreDatabase);
        _active = new(eventStoreDatabase);
        var first = new EventType("event", 1);
        var second = new EventType("event", 2);
        var definition = new ProjectionDefinition(ProjectionOwner.Client, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<EventType, FromDefinition> { [first] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null), [second] = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null) }, new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>()) { Version = "1.0.0", LastUpdated = DateTimeOffset.FromUnixTimeSeconds(90), GenerationDelivery = EventGenerationDelivery.Pinned };
        _entry = new(definition.Identifier, new string('A', 64), "definition-content-sha256/v1", definition.Version, DateTimeOffset.FromUnixTimeSeconds(100), definition);
    }

    protected async Task Read() => _entries = (await _storage.GetFor(_entry.Projection)).ToArray();
    Task Destroy() => _client.DropDatabaseAsync(_databaseName);
}
