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
using MongoDB.Driver;

using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_compliance_changes_nested_property_after_complex_property_is_set.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_compliance_changes_nested_property_after_complex_property_is_set(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        const string UserId = "user-1";
        const string PlainDisplayName = "Ada Lovelace";
        const string EncryptedDisplayName = "encrypted-display-name";

        IMongoClient _client = default!;
        IMongoDatabase _database = default!;
        Sink _sink = default!;
        Key _key = default!;
        string _databaseName = default!;

        public Exception? Error;
        public ExpandoObject? Result;

        public async Task InitializeAsync()
        {
            _databaseName = $"chronicle_sink_specs_{Guid.NewGuid():N}";
            _client = new MongoClient(fixture.ConnectionString);
            _database = _client.GetDatabase(_databaseName);
            _key = new Key(UserId, ArrayIndexers.NoIndexers);

            var readModel = CreateReadModelDefinition();
            var typeFormats = new TypeFormats();
            var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
            var collections = new SinkCollections(readModel, _database);
            var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, readModel);
            var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, expandoObjectConverter);
            _sink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, expandoObjectConverter);

            try
            {
                await _sink.ApplyChanges(_key, CreateChangeset(), EventSequenceNumber.First);
            }
            catch (Exception error)
            {
                Error = error;
            }

            Result = await _sink.FindOrDefault(_key);
        }

        public async Task DisposeAsync()
        {
            if (_databaseName is not null)
            {
                await _client.DropDatabaseAsync(_databaseName);
            }
        }

        public IDictionary<string, object?> GetDisplayName()
        {
            var result = (IDictionary<string, object?>)Result!;
            return (IDictionary<string, object?>)result["displayName"]!;
        }

        static IChangeset<AppendedEvent, ExpandoObject> CreateChangeset()
        {
            var plainDisplayName = Expando(
                ("name", PlainDisplayName),
                ("verificationLevel", 1));
            var encryptedDisplayName = Expando(
                ("name", EncryptedDisplayName),
                ("verificationLevel", 1));

            var projectedState = Expando(("displayName", plainDisplayName));
            var encryptedState = Expando(("displayName", encryptedDisplayName));
            var displayNameSet = new PropertiesChanged<ExpandoObject>(
                projectedState,
                [new PropertyDifference("displayName", null, plainDisplayName)]);
            var nestedEncrypted = new PropertiesChanged<ExpandoObject>(
                encryptedState,
                [new PropertyDifference("displayName.name", PlainDisplayName, EncryptedDisplayName)]);

            var changeset = new Changeset<AppendedEvent, ExpandoObject>(
                new ObjectComparer(),
                CreateEvent(),
                new ExpandoObject());
            changeset.Add(displayNameSet);
            changeset.Add(nestedEncrypted);

            return changeset;
        }

        static AppendedEvent CreateEvent()
        {
            var context = EventContext.From(
                "test-store",
                "test-namespace",
                EventType.Unknown,
                EventSourceType.Default,
                UserId,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet);

            return new AppendedEvent(context, new ExpandoObject());
        }

        static ReadModelDefinition CreateReadModelDefinition() =>
            new(
                "test-user-profile-read-model",
                "TestUserProfileReadModel",
                $"user_profiles_{Guid.NewGuid():N}",
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
                    "displayName": {
                      "type": "object",
                      "properties": {
                        "name": { "type": "string" },
                        "verificationLevel": { "type": "integer" }
                      },
                      "required": ["name", "verificationLevel"]
                    }
                  },
                  "required": ["id", "displayName"]
                }
                """);

        static ExpandoObject Expando(params (string Key, object? Value)[] properties)
        {
            var result = new ExpandoObject();
            var dictionary = (IDictionary<string, object?>)result;
            foreach (var (key, value) in properties)
            {
                dictionary[key] = value;
            }

            return result;
        }
    }

    [Fact] void should_not_throw_from_mongodb() => ctx.Error.ShouldBeNull();
    [Fact] void should_find_the_user_profile() => ctx.Result.ShouldNotBeNull();
    [Fact] void should_store_the_encrypted_nested_value() => ctx.GetDisplayName()["name"].ShouldEqual("encrypted-display-name");
    [Fact] void should_keep_the_other_complex_property_values() => ctx.GetDisplayName()["verificationLevel"].ShouldEqual(1);
}
