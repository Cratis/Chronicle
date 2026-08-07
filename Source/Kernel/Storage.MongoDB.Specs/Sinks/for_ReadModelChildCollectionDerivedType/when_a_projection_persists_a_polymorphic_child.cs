// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionDerivedType;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3571 — a child of a polymorphic
/// (<c>[DerivedType]</c>) collection carrying a <c>_derivedTypeId</c> discriminator must round-trip
/// through the real MongoDB sink unchanged. The read model's item schema is an open object (no fixed
/// "properties", as <c>JsonSchemaGenerator</c> emits for a type with registered derivatives), so the
/// sink must preserve every property it does not recognize rather than dropping it.
/// </summary>
/// <param name="ctx">The shared fixture holding the stored child document.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_projection_persists_a_polymorphic_child(when_a_projection_persists_a_polymorphic_child.context ctx)
    : IClassFixture<when_a_projection_persists_a_polymorphic_child.context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        const string Identifier = "slice-1";

        IMongoClient _client = default!;
        string _databaseName = default!;

        public BsonDocument? StoredChild { get; private set; }

        public async Task InitializeAsync()
        {
            var schema = await JsonSchema.FromJsonAsync(
                """
                {
                  "type": "object",
                  "properties": {
                    "id": { "type": "string" },
                    "actors": {
                      "type": "array",
                      "items": {
                        "type": "object"
                      }
                    }
                  }
                }
                """);

            var typeFormats = new TypeFormats();
            var sinkConverter = new ExpandoObjectConverter(typeFormats);

            _databaseName = $"chronicle_polymorphic_child_{Guid.NewGuid():N}";
            _client = new MongoClient(fixture.ConnectionString);
            var database = _client.GetDatabase(_databaseName);

            var readModel = new ReadModelDefinition(
                "polymorphic-child-read-model",
                $"slices_{Guid.NewGuid():N}",
                "PolymorphicChildReadModel",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Projection,
                ReadModelObserverIdentifier.Unspecified,
                SinkDefinition.None,
                new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } },
                []);

            var collections = new SinkCollections(readModel, database);
            var mongoDBConverter = new MongoDBConverter(sinkConverter, typeFormats, readModel);
            var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, sinkConverter, NullLogger<ChangesetConverter>.Instance);
            var sink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, sinkConverter);

            var key = new Key(Identifier, ArrayIndexers.NoIndexers);

            var actor = new ExpandoObject();
            var actorValues = (IDictionary<string, object?>)actor;
            actorValues["_derivedTypeId"] = "userExperience";
            actorValues["actorId"] = "actor-1";
            actorValues["displayName"] = "Jane";

            var objectComparer = new ObjectComparer();
            var @event = new AppendedEvent(
                EventContext.From(
                    "test-store",
                    "test-namespace",
                    EventType.Unknown,
                    EventSourceType.Default,
                    Identifier,
                    EventStreamType.All,
                    EventStreamId.Default,
                    EventSequenceNumber.First,
                    CorrelationId.NotSet),
                new ExpandoObject());
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, new ExpandoObject());
            changeset.AddChild("actors", actor);

            await sink.ApplyChanges(key, changeset, EventSequenceNumber.First);

            var storedDocument = await collections.GetCollection()
                .Find(Builders<BsonDocument>.Filter.Empty)
                .FirstAsync();
            StoredChild = storedDocument["actors"].AsBsonArray[0].AsBsonDocument;
        }

        public async Task DisposeAsync()
        {
            if (_databaseName is not null)
            {
                await _client.DropDatabaseAsync(_databaseName);
            }
        }
    }

    [Fact] void should_preserve_the_derived_type_discriminator() => ctx.StoredChild!.Contains("_derivedTypeId").ShouldBeTrue();
    [Fact] void should_preserve_other_actor_fields() => ctx.StoredChild!["displayName"].AsString.ShouldEqual("Jane");
}
