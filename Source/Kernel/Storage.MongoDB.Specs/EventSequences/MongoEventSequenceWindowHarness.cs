// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Runs the shared window contract against MongoDB with real append and cursor conversion.
/// </summary>
public class MongoEventSequenceWindowHarness : IEventSequenceWindowHarness
{
    IMongoClient? _client;
    string? _databaseName;

    /// <summary>
    /// Gets the replica-set fixture providing the database.
    /// </summary>
    public ReplicaSetMongoDBFixture Fixture { get; init; } = null!;

    /// <inheritdoc/>
    public Task<IEventSequenceStorage> CreateStorage()
    {
        _databaseName = MongoDBSpecDatabaseNames.New();
        _client = new MongoClient(Fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.Client.Returns(_client);
        namespaceDatabase.GetEventSequenceCollectionFor(Arg.Any<EventSequenceId>()).Returns(database.GetCollection<Event>("event-log"));
        namespaceDatabase.GetEventSequenceCollectionAsBsonFor(Arg.Any<EventSequenceId>()).Returns(database.GetCollection<BsonDocument>("event-log"));
        var eventTypes = Substitute.For<IEventTypesStorage>();
        eventTypes.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(call =>
            new EventTypeSchema(new EventType(call.Arg<EventTypeId>(), EventTypeGeneration.First), EventTypeOwner.None, EventTypeSource.Unknown, new JsonSchema()));
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        return Task.FromResult<IEventSequenceStorage>(new EventSequenceStorage(
            "window-store",
            "window-namespace",
            EventSequenceId.Log,
            namespaceDatabase,
            new EventConverter("window-store", "window-namespace", eventTypes, identities, converter),
            eventTypes,
            identities,
            converter,
            new JsonSerializerOptions(),
            Substitute.For<ILogger<EventSequenceStorage>>()));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_client is not null && _databaseName is not null)
        {
            _client.DropDatabase(_databaseName);
            _client.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
