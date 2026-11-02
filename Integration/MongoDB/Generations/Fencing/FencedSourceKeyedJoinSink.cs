// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Generations.Fencing;

public class FencedSourceKeyedJoinSink(ChronicleInProcessFixture fixture) : Integration.given.a_mongo_client(fixture)
{
    protected const string JoinedStream = "b1";
    protected const string Applied = "__applied";
    protected static readonly Key RootKey = new("root", ArrayIndexers.NoIndexers);
    protected static readonly string DefinitionId = AppliedEventMarkers.DefinitionId("joined", "joinId");
    protected static readonly Cratis.Chronicle.Concepts.EventStoreNamespaceName Namespace = "default";

    protected readonly string _databaseName = $"fenced_marker_{Guid.NewGuid():N}";
    protected IMongoCollection<BsonDocument> _documents;
    protected FencedSinkWrites _writes;
    protected Sink _sink;

    async Task Establish()
    {
        var database = _client.GetDatabase(_databaseName);
        await database.CreateCollectionAsync("models");
        _documents = database.GetCollection<BsonDocument>("models");
        await _documents.InsertManyAsync(
        [
            new BsonDocument { ["_id"] = "root", ["joinId"] = JoinedStream, ["count"] = 0 },
            new BsonDocument { ["_id"] = "other", ["joinId"] = JoinedStream, ["count"] = 0 }
        ]);
        var gates = new FenceGates(database);
        var token = await gates.Claim("integration/models", 1);
        _writes = new FencedSinkWrites(new FixedFenceTokenSource(token), new FencedBatchExecutor(new MongoFenceStore(database), FenceRetryPolicy.Default, Substitute.For<IFencedBatchProbe>()));
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"joinId":{"type":"string"},"count":{"type":"integer"}}}
            """);
        var readModel = new ReadModelDefinition("model", "models", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var collections = new SinkCollections(readModel, database, null, _writes);
        var converter = new MongoDBConverter(expando, formats, readModel, Microsoft.Extensions.Logging.Abstractions.NullLogger<MongoDBConverter>.Instance);
        var changes = new ChangesetConverter(readModel, converter, collections, expando, Microsoft.Extensions.Logging.Abstractions.NullLogger<ChangesetConverter>.Instance, _writes);
        _sink = new Sink(readModel, converter, collections, changes, expando, new ReadModelChangeStreams(Microsoft.Extensions.Logging.Abstractions.NullLogger<ReadModelChangeStreams>.Instance), Microsoft.Extensions.Logging.Abstractions.NullLogger<Sink>.Instance, _writes);
    }

    protected static AppendedEvent EventOf(string eventTypeId, string eventSourceId, ulong sequenceNumber) =>
        new(
            EventContext.Empty with
            {
                EventType = new EventType(eventTypeId, EventTypeGeneration.First),
                EventSourceId = eventSourceId,
                Namespace = Namespace,
                SequenceNumber = sequenceNumber
            },
            new ExpandoObject());

    protected static Change Direct(int count) =>
        new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath("count"), null, count)]);

    protected static Change JoinOf(int count) =>
        new Joined(new ExpandoObject(), JoinedStream, new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(count)]);

    protected Task Apply(AppendedEvent incoming, params Change[] changes)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns(changes);
        changeset.Incoming.Returns(incoming);
        changeset.HasJoined().Returns(changes.OfType<Joined>().Any());
        return _sink.ApplyChanges(RootKey, changeset, incoming.Context.SequenceNumber, SinkWriteMode.OncePerSourceEvent);
    }

    protected Task Clear(string partition) =>
        (_sink as ISinkAppliedEventMarkers ?? throw new InvalidOperationException("The sink does not clear applied event markers"))
            .ClearAppliedEventMarkers(Namespace, partition, [new AppliedEventMarkerJoin(DefinitionId, "joined", "joinId")]);

    protected Task<BsonDocument> Read(string id) => _documents.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();

    async Task Destroy()
    {
        _writes?.Dispose();
        await _client.DropDatabaseAsync(_databaseName);
    }
}
