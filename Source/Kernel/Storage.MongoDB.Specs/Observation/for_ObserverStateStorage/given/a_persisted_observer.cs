// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.given;

public class a_persisted_observer(MongoDBFixture fixture) : Specification
{
    protected static readonly EventTypeId _type = new("event-a");
    protected ObserverStateStorage _storage;
    protected ObserverTypeSetControl _control;
    protected Chronicle.Storage.Observation.ObserverState _saved;
    IMongoClient _client;
    string _databaseName;

    async Task Establish()
    {
        _databaseName = MongoDBSpecDatabaseNames.New();
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var collection = database.GetCollection<ObserverState>(WellKnownCollectionNames.Observers);
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.GetObserverStateCollection().Returns(collection);
        _storage = new(namespaceDatabase);
        _control = new(
            new(7),
            new("$reactor-subscriber", Concepts.Observation.ObserverFilters.None),
            new Dictionary<EventTypeId, ObserverTypeSetMember> { [_type] = new(new(7), new(42)) },
            [new("$connection-new", null, [new(_type, new(2))], EventGenerationDelivery.Pinned)])
        {
            RetirementResults = [new(Guid.NewGuid(), _type, ObserverEventTypeRetirementOutcome.Retired, new(3), [])],
            PendingChange = new(new(8), DateTimeOffset.UtcNow, new(42), [new("event-b")], [])
        };
        await _storage.Save(new() { Identifier = "observer", TypeSetControl = _control }, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
