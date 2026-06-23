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

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionCompliance;

/// <summary>
/// Regression for child-collection PII being written to the sink in the clear. A projection that adds a
/// child carrying a <c>[PII]</c> member goes through the real projection encryption step (EncryptChangeset)
/// and a real MongoDB sink; the stored child element must be ciphertext at rest and release back to the
/// original plaintext, mirroring how the read path decrypts per array element.
/// </summary>
/// <param name="ctx">The shared fixture holding the projected and re-read values.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_projection_persists_a_child_collection_with_pii(when_a_projection_persists_a_child_collection_with_pii.context ctx)
    : IClassFixture<when_a_projection_persists_a_child_collection_with_pii.context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        public const string PlaintextName = "Jane Doe";
        const string EventStore = "test-store";
        const string EventStoreNamespace = "test-namespace";
        const string Identifier = "contact-owner-1";

        IMongoClient _client = default!;
        string _databaseName = default!;

        public string StoredChildName { get; private set; } = string.Empty;

        public bool StoredChildNameIsBase64 { get; private set; }

        public string ReleasedChildName { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            var schema = await JsonSchema.FromJsonAsync(
                """
                {
                  "type": "object",
                  "properties": {
                    "id": { "type": "string" },
                    "contacts": {
                      "type": "array",
                      "items": {
                        "type": "object",
                        "properties": {
                          "contactId": { "type": "string" },
                          "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] },
                          "status": { "type": "string" }
                        }
                      }
                    }
                  }
                }
                """);

            var typeFormats = new TypeFormats();
            var sinkConverter = new ExpandoObjectConverter(typeFormats);
            var complianceConverter = new Cratis.Chronicle.Json.ExpandoObjectConverter(typeFormats);
            var complianceManager = new JsonComplianceManager(
                new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(
                    new PIICompliancePropertyValueHandler(new InMemoryEncryptionKeyStorage(), new Encryption())));
            var compliance = new ReadModelsCompliance(complianceManager, complianceConverter);
            var objectComparer = new ObjectComparer();

            _databaseName = $"chronicle_child_pii_{Guid.NewGuid():N}";
            _client = new MongoClient(fixture.ConnectionString);
            var database = _client.GetDatabase(_databaseName);

            var readModel = new ReadModelDefinition(
                "child-pii-read-model",
                $"contacts_{Guid.NewGuid():N}",
                "ChildPiiReadModel",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Projection,
                ReadModelObserverIdentifier.Unspecified,
                SinkDefinition.None,
                new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } },
                []);

            var collections = new SinkCollections(readModel, database);
            var mongoDBConverter = new MongoDBConverter(sinkConverter, typeFormats, readModel);
            var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, sinkConverter);
            var sink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, sinkConverter);

            var projection = Substitute.For<IProjection>();
            projection.TargetReadModelSchema.Returns(schema);
            var step = new EncryptChangeset(compliance, objectComparer, EventStore, EventStoreNamespace);

            var key = new Key(Identifier, ArrayIndexers.NoIndexers);
            var @event = new AppendedEvent(
                EventContext.From(
                    EventStore,
                    EventStoreNamespace,
                    EventType.Unknown,
                    EventSourceType.Default,
                    Identifier,
                    EventStreamType.All,
                    EventStreamId.Default,
                    EventSequenceNumber.First,
                    CorrelationId.NotSet),
                new ExpandoObject());

            var contact = new ExpandoObject();
            var contactValues = (IDictionary<string, object?>)contact;
            contactValues["contactId"] = "contact-1";
            contactValues["name"] = PlaintextName;
            contactValues["status"] = "active";

            var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, new ExpandoObject());
            changeset.AddChild("contacts", contact);

            var projectionContext = new ProjectionEventContext(key, @event, changeset, ProjectionOperationType.None, false);

            await step.Perform(projection, projectionContext);
            await sink.ApplyChanges(key, changeset, EventSequenceNumber.First);

            var storedDocument = await collections.GetCollection()
                .Find(Builders<BsonDocument>.Filter.Empty)
                .FirstAsync();
            var storedChild = storedDocument["contacts"].AsBsonArray[0].AsBsonDocument;
            StoredChildName = storedChild["name"].AsString;
            StoredChildNameIsBase64 = IsBase64(StoredChildName);

            // Release the stored child element through the compliance manager using the child element schema,
            // mirroring the per-array-element decryption the read path performs. With the child encrypted at
            // rest this round-trips to the original plaintext; were it persisted in the clear, this release
            // would throw trying to base64-decode plaintext and fail the spec.
            var childSchema = schema.GetSchemaForPropertyPath("contacts");
            var releasedChild = await complianceManager.Release(
                EventStore,
                EventStoreNamespace,
                childSchema,
                Identifier,
                new JsonObject
                {
                    ["contactId"] = storedChild["contactId"].AsString,
                    ["name"] = storedChild["name"].AsString,
                    ["status"] = storedChild["status"].AsString
                });
            ReleasedChildName = releasedChild["name"]!.GetValue<string>();
        }

        public async Task DisposeAsync()
        {
            if (_databaseName is not null)
            {
                await _client.DropDatabaseAsync(_databaseName);
            }
        }

        static bool IsBase64(string value)
        {
            Span<byte> buffer = new byte[value.Length];
            return Convert.TryFromBase64String(value, buffer, out _);
        }
    }

    [Fact] void should_store_child_pii_as_ciphertext_not_plaintext() => ctx.StoredChildName.ShouldNotEqual(context.PlaintextName);

    [Fact] void should_store_child_pii_as_base64_ciphertext() => ctx.StoredChildNameIsBase64.ShouldBeTrue();

    [Fact] void should_release_child_pii_back_to_the_original_plaintext() => ctx.ReleasedChildName.ShouldEqual(context.PlaintextName);
}
