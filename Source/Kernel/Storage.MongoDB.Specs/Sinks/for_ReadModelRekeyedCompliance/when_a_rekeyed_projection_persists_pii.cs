// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelRekeyedCompliance;

/// <summary>
/// Regression for a re-keyed projection returning <c>[PII]</c> as ciphertext on read. The read model is
/// keyed by a property other than the source event's event source id, so the document key (<c>_id</c>) and
/// the source event's compliance subject diverge. The document must encrypt PII under, and stamp its
/// <c>__subject</c> as, its own resolved key — otherwise the identity the PII is encrypted under is not the
/// one the read path releases it with, and the value never decrypts. Runs the real projection encryption
/// step through a real MongoDB sink.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_rekeyed_projection_persists_pii(MongoDBFixture fixture) : Specification
{
    const string EventStore = "test-store";
    const string EventStoreNamespace = "test-namespace";
    const string DocumentKey = "member-1";
    const string SourceEventSourceId = "group-1";
    const string PlaintextName = "Ada Lovelace";

    IMongoClient _client = default!;
    string _databaseName = default!;
    SinkCollections _collections = default!;
    Sink _sink = default!;
    JsonComplianceManager _complianceManager = default!;
    ReadModelsCompliance _compliance = default!;
    JsonSchema _schema = default!;
    Key _key = default!;
    EncryptChangeset _step = default!;
    IProjection _projection = default!;

    string _storedId = default!;
    string? _storedSubject;
    string _storedName = default!;
    string? _releasedNameUnderDocumentKey;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "id": { "type": "string" },
                "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] }
              }
            }
            """);

        var typeFormats = new TypeFormats();
        var sinkConverter = new ExpandoObjectConverter(typeFormats);
        var complianceConverter = new Cratis.Chronicle.Json.ExpandoObjectConverter(typeFormats);
        _complianceManager = new JsonComplianceManager(
            new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(
                new PIICompliancePropertyValueHandler(new InMemoryEncryptionKeyStorage(), new Encryption())));
        _compliance = new ReadModelsCompliance(_complianceManager, complianceConverter);

        _databaseName = $"chronicle_rekeyed_pii_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);

        var readModel = new ReadModelDefinition(
            "rekeyed-pii-read-model",
            $"rekeyed_pii_{Guid.NewGuid():N}",
            "RekeyedPiiReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, _schema } },
            []);

        _collections = new SinkCollections(readModel, database);
        var mongoDBConverter = new MongoDBConverter(sinkConverter, typeFormats, readModel);
        var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, _collections, sinkConverter);
        _sink = new Sink(readModel, mongoDBConverter, _collections, changesetConverter, sinkConverter);

        _projection = Substitute.For<IProjection>();
        _projection.TargetReadModelSchema.Returns(_schema);
        _step = new EncryptChangeset(_compliance, new ObjectComparer(), EventStore, EventStoreNamespace);
        _key = new Key(DocumentKey, ArrayIndexers.NoIndexers);
    }

    async Task Because()
    {
        // The projected state for the re-keyed document, built from the decrypted event.
        var state = new ExpandoObject();
        var stateValues = (IDictionary<string, object?>)state;
        stateValues["id"] = DocumentKey;
        stateValues["name"] = PlaintextName;

        // The event was appended to the parent (group) stream, so its event source id / subject is the group,
        // not the member the read model is re-keyed by.
        var @event = new AppendedEvent(
            EventContext.From(
                EventStore,
                EventStoreNamespace,
                EventType.Unknown,
                EventSourceType.Default,
                SourceEventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet),
            new ExpandoObject());

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), @event, state);
        var context = new ProjectionEventContext(_key, @event, changeset, ProjectionOperationType.None, false);

        await _step.Perform(_projection, context);
        await _sink.ApplyChanges(_key, changeset, EventSequenceNumber.First);

        var stored = await _collections.GetCollection().Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();
        _storedId = stored["_id"].AsString;
        _storedSubject = stored.Contains(WellKnownProperties.Subject) ? stored[WellKnownProperties.Subject].AsString : null;
        _storedName = stored["name"].AsString;

        // Release the stored PII under the document's own key — the identity a re-keyed document is stored
        // under. When the encryption identity matches the document key this returns the plaintext; when they
        // diverge (the bug) the key for the document id does not exist and release fails.
        try
        {
            var released = await _complianceManager.Release(
                EventStore,
                EventStoreNamespace,
                _schema,
                _storedId,
                new JsonObject { ["name"] = _storedName });
            _releasedNameUnderDocumentKey = released["name"]!.GetValue<string>();
        }
        catch
        {
            _releasedNameUnderDocumentKey = null;
        }
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    [Fact] void should_store_pii_as_ciphertext_not_plaintext() => _storedName.ShouldNotEqual(PlaintextName);

    [Fact] void should_stamp_the_document_subject_as_the_resolved_key() => _storedSubject.ShouldEqual(_storedId);

    [Fact] void should_encrypt_pii_under_the_document_key() => _releasedNameUnderDocumentKey.ShouldEqual(PlaintextName);
}
