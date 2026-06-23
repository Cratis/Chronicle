// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChildCollectionCompliance.given;

/// <summary>
/// Base for child-collection PII regression scenarios. Builds a real MongoDB sink wired to the real
/// compliance machinery (encrypt-at-rest plus release), so a derived scenario can drive a projection or
/// reducer through the full encrypt -> store -> read -> release round trip that the in-memory sink and
/// the passthrough-compliance parity suite do not exercise. <c>Establish</c> builds the sink stack; the
/// derived spec drives the scenario in its own <c>Because</c>.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
public abstract class a_child_collection_compliance_scenario(MongoDBFixture fixture) : Specification
{
    protected const string EventStore = "test-store";
    protected const string EventStoreNamespace = "test-namespace";

    IMongoClient _client = default!;
    string _databaseName = default!;

    /// <summary>Gets the read-model schema, loaded from <see cref="SchemaJson"/>.</summary>
    protected JsonSchema Schema { get; private set; } = default!;

    /// <summary>Gets the comparer used to compute changesets.</summary>
    protected ObjectComparer ObjectComparer { get; } = new();

    /// <summary>Gets the compliance manager performing the actual encryption and release.</summary>
    protected JsonComplianceManager ComplianceManager { get; private set; } = default!;

    /// <summary>Gets the read-model compliance facade over <see cref="ComplianceManager"/>.</summary>
    protected ReadModelsCompliance Compliance { get; private set; } = default!;

    /// <summary>Gets the sink collections for the read model.</summary>
    protected SinkCollections Collections { get; private set; } = default!;

    /// <summary>Gets the real MongoDB sink under test.</summary>
    protected Sink Sink { get; private set; } = default!;

    /// <summary>Gets the read-model definition the sink and pipelines are built for.</summary>
    protected ReadModelDefinition ReadModel { get; private set; } = default!;

    /// <summary>Gets the key the read model is stored under.</summary>
    protected Key Key { get; private set; } = default!;

    /// <summary>Gets the identifier (event source id / compliance subject) for the read model.</summary>
    protected virtual string Identifier => "subject-1";

    /// <summary>Gets the read-model schema as JSON.</summary>
    protected abstract string SchemaJson { get; }

    /// <summary>Gets the observer type the read model is built by.</summary>
    protected abstract ReadModelObserverType ObserverType { get; }

    async Task Establish()
    {
        Schema = await JsonSchema.FromJsonAsync(SchemaJson);
        Key = new Key(Identifier, ArrayIndexers.NoIndexers);

        var typeFormats = new TypeFormats();
        var sinkConverter = new ExpandoObjectConverter(typeFormats);
        var complianceConverter = new Cratis.Chronicle.Json.ExpandoObjectConverter(typeFormats);
        ComplianceManager = new JsonComplianceManager(
            new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(
                new PIICompliancePropertyValueHandler(new InMemoryEncryptionKeyStorage(), new Encryption())));
        Compliance = new ReadModelsCompliance(ComplianceManager, complianceConverter);

        _databaseName = $"chronicle_child_collection_pii_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);

        ReadModel = new ReadModelDefinition(
            "child-collection-pii-read-model",
            $"child_collection_pii_{Guid.NewGuid():N}",
            "ChildCollectionPiiReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ObserverType,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, Schema } },
            []);

        Collections = new SinkCollections(ReadModel, database);
        var mongoDBConverter = new MongoDBConverter(sinkConverter, typeFormats, ReadModel);
        var changesetConverter = new ChangesetConverter(ReadModel, mongoDBConverter, Collections, sinkConverter);
        Sink = new Sink(ReadModel, mongoDBConverter, Collections, changesetConverter, sinkConverter);
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    /// <summary>Reads the single stored read-model document straight from MongoDB.</summary>
    /// <returns>The stored <see cref="BsonDocument"/>.</returns>
    protected Task<BsonDocument> StoredDocument() =>
        Collections.GetCollection().Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();

    protected static ExpandoObject Expando(params (string Key, object? Value)[] properties)
    {
        var instance = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)instance;
        foreach (var (key, value) in properties)
        {
            dictionary[key] = value;
        }

        return instance;
    }

    protected static bool IsBase64(string value)
    {
        Span<byte> buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }
}
