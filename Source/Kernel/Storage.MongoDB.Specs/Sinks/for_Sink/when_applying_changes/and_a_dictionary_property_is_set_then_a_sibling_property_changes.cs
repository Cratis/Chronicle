// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_a_dictionary_property_is_set_then_a_sibling_property_changes.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

/// <summary>
/// Regression coverage for https://github.com/Cratis/Chronicle/issues/3568 - a dictionary-shaped
/// (additionalProperties) property nested under a [Nested] object used to be silently corrupted into
/// a List&lt;KeyValuePair&lt;,&gt;&gt; the moment the containing object's state was cloned to compute the
/// next event's changeset, which serialized as an array of { Key, Value } documents instead of a BSON
/// document, and could make the following update to a sibling property look like a no-op.
/// </summary>
/// <param name="ctx">The <see cref="context"/> for the spec.</param>
[Collection(MongoDBCollection.Name)]
public class and_a_dictionary_property_is_set_then_a_sibling_property_changes(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        const string ItemId = "item-1";

        readonly ObjectComparer _objectComparer = new();

        IMongoClient _client = default!;
        IMongoDatabase _database = default!;
        IMongoCollection<BsonDocument> _collection = default!;
        Sink _sink = default!;
        JsonSchema _schema = default!;
        Key _key = default!;
        string _databaseName = default!;

        public BsonDocument? RawDocumentAfterFirstWrite;
        public ExpandoObject? Result;

        public async Task InitializeAsync()
        {
            _databaseName = $"chronicle_sink_specs_{Guid.NewGuid():N}";
            _client = new MongoClient(fixture.ConnectionString);
            _database = _client.GetDatabase(_databaseName);
            _schema = CreateSchema();
            _key = new Key(ItemId, ArrayIndexers.NoIndexers);

            var readModel = CreateReadModelDefinition();
            var typeFormats = new TypeFormats();
            var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
            var collections = new SinkCollections(readModel, _database);
            var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, readModel);
            var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, expandoObjectConverter, NullLogger<ChangesetConverter>.Instance);
            _sink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, expandoObjectConverter);
            _collection = collections.GetCollection();

            // First event: sets the whole "definition" nested object, including a dictionary-shaped
            // "entries" property - exactly how a [SetFrom<DefinitionSet>] property mapper populates it,
            // straight from the event's own parsed content, without cloning.
            var definition = CreateDefinition("first title", ("first", "firstValue"));
            var firstChangeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, CreateEvent(EventSequenceNumber.First), new ExpandoObject());
            firstChangeset.SetProperties(
                [PropertyMappers.FromEventValueProvider(new PropertyPath("definition"), _ => definition)],
                ArrayIndexers.NoIndexers);
            await _sink.ApplyChanges(_key, firstChangeset, EventSequenceNumber.First);

            RawDocumentAfterFirstWrite = await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", ItemId)).SingleAsync();

            // Second event: only renames the sibling "title" property. This is where the initial state
            // gets read back and cloned by Changeset.SetProperties before the property mapper runs.
            var initial = await _sink.FindOrDefault(_key);
            var secondChangeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, CreateEvent(EventSequenceNumber.First + 1), initial!);
            secondChangeset.SetProperties(
                [PropertyMappers.FromEventValueProvider(new PropertyPath("definition.title"), _ => "second title")],
                ArrayIndexers.NoIndexers);
            await _sink.ApplyChanges(_key, secondChangeset, EventSequenceNumber.First + 1);

            Result = await _sink.FindOrDefault(_key);
        }

        public async Task DisposeAsync()
        {
            if (_databaseName is not null)
            {
                await _client.DropDatabaseAsync(_databaseName);
            }
        }

        public IDictionary<string, object?> GetDefinition()
        {
            var result = (IDictionary<string, object?>)Result!;
            return (IDictionary<string, object?>)result["definition"]!;
        }

        static ExpandoObject CreateDefinition(string title, params (string Key, string Value)[] entries)
        {
            var dictionary = new Dictionary<object, object>();
            foreach (var (key, value) in entries)
            {
                dictionary[key] = value;
            }

            dynamic definition = new ExpandoObject();
            definition.title = title;
            definition.entries = dictionary;
            return definition;
        }

        static AppendedEvent CreateEvent(EventSequenceNumber sequenceNumber)
        {
            var context = EventContext.From(
                "test-store",
                "test-namespace",
                EventType.Unknown,
                EventSourceType.Default,
                ItemId,
                EventStreamType.All,
                EventStreamId.Default,
                sequenceNumber,
                CorrelationId.NotSet);

            return new AppendedEvent(context, new ExpandoObject());
        }

        static ReadModelDefinition CreateReadModelDefinition() =>
            new(
                "test-item-read-model",
                "TestItemReadModel",
                $"items_{Guid.NewGuid():N}",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Projection,
                ReadModelObserverIdentifier.Unspecified,
                SinkDefinition.None,
                new Dictionary<ReadModelGeneration, JsonSchema>
                {
                    { ReadModelGeneration.First, CreateSchema() }
                },
                []);

        static JsonSchema CreateSchema() =>
            JsonSchema.FromJson(
                """
                {
                  "type": "object",
                  "properties": {
                    "id": { "type": "string" },
                    "definition": {
                      "type": "object",
                      "properties": {
                        "title": { "type": "string" },
                        "entries": {
                          "type": "object",
                          "additionalProperties": { "type": "string" }
                        }
                      },
                      "required": ["title", "entries"]
                    }
                  },
                  "required": ["id", "definition"]
                }
                """);
    }

    [Fact] void should_store_entries_as_a_document_after_the_first_write() =>
        ctx.RawDocumentAfterFirstWrite!["definition"].AsBsonDocument["entries"].BsonType.ShouldEqual(BsonType.Document);
    [Fact] void should_have_updated_the_title() => ((string)ctx.GetDefinition()["title"]!).ShouldEqual("second title");
    [Fact] void should_still_have_the_entries_property() => ctx.GetDefinition()["entries"].ShouldNotBeNull();
    [Fact] void should_preserve_the_dictionary_entry_after_the_sibling_update() =>
        ((IDictionary<object, object>)ctx.GetDefinition()["entries"]!)["first"].ShouldEqual("firstValue");
}
