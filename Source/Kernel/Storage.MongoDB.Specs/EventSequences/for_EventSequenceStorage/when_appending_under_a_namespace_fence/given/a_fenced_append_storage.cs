// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_under_a_namespace_fence.given;

public class a_fenced_append_storage : Namespaces.for_NamespaceEventTransaction.given.a_namespace_transaction
{
    protected readonly List<IClientSessionHandle> _lookupSessions = [];
    protected INamespaceFencedEventSequenceStorage _storage;
    protected IMongoCollection<Event> _events;
    protected EventToAppendToStorage _event;
    protected Event[] _inserted = [];
    protected Event[] _foundEvents = [];
    protected bool _slotOccupied;

    protected static MongoWriteException Duplicate()
    {
        var write = (WriteError)Activator.CreateInstance(typeof(WriteError), BindingFlags.Instance | BindingFlags.NonPublic, null, [ServerErrorCategory.DuplicateKey, 11000, "Duplicate key", new BsonDocument()], null)!;
        var connection = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017)));
        return new MongoWriteException(connection, write, null, null);
    }

    static IAsyncCursor<T> Cursor<T>(IEnumerable<T> items)
    {
        var cursor = Substitute.For<IAsyncCursor<T>>();
        cursor.Current.Returns(items);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }

    async Task Establish()
    {
        _events = Substitute.For<IMongoCollection<Event>>();
        _database.GetEventSequenceCollectionFor(EventSequenceId.Log).Returns(_events);
        _events.InsertOneAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<Event>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Insert(call.Arg<IClientSessionHandle>(), [call.Arg<Event>()], call.Arg<CancellationToken>()));
        _events.InsertManyAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<IEnumerable<Event>>(), Arg.Any<InsertManyOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Insert(call.Arg<IClientSessionHandle>(), call.Arg<IEnumerable<Event>>().ToArray(), call.Arg<CancellationToken>()));
        _events.FindAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<Event>>(), Arg.Any<FindOptions<Event, Event>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _lookupSessions.Add(call.Arg<IClientSessionHandle>());
                return Task.FromResult(Cursor(_foundEvents));
            });
        _events.FindAsync(Arg.Any<IClientSessionHandle>(), Arg.Any<FilterDefinition<Event>>(), Arg.Any<FindOptions<Event, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _lookupSessions.Add(call.Arg<IClientSessionHandle>());
                return Task.FromResult(Cursor<BsonDocument>(_slotOccupied ? [new BsonDocument()] : []));
            });
        var eventType = new EventType("fenced-event", 1);
        var schema = await JsonSchema.FromJsonAsync("{}");
        var types = Substitute.For<IEventTypesStorage>();
        types.GetFor(eventType.Id, eventType.Generation).Returns(new EventTypeSchema(eventType, EventTypeOwner.None, EventTypeSource.Unknown, schema));
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        var converter = Substitute.For<Json.IExpandoObjectConverter>();
        converter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());
        _storage = new EventSequenceStorage("store", "namespace", EventSequenceId.Log, _database, Substitute.For<IEventConverter>(), types, identities, converter, new JsonSerializerOptions(), Substitute.For<ILogger<EventSequenceStorage>>());
        _event = new(7, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, eventType, CorrelationId.NotSet, [], [IdentityId.NotSet], [], DateTimeOffset.UnixEpoch.AddTicks(12345), new ExpandoObject(), "hash")
        {
            Identity = new("identity", "fingerprint"),
            CompensationFor = 3,
            NamedTags = [new("region", "east")]
        };
    }

    Task Insert(IClientSessionHandle session, Event[] events, CancellationToken cancellationToken)
    {
        _steps.Add("insert");
        _writeSessions.Add(session);
        _writeCancellation = cancellationToken;
        _inserted = events;
        _duringWrite?.Invoke();
        if (_writeFailure is not null) throw _writeFailure;
        return Task.CompletedTask;
    }
}
