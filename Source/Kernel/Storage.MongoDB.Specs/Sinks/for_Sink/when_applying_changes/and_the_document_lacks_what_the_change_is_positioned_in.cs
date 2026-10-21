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
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_the_document_lacks_what_the_change_is_positioned_in.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

/// <summary>
/// A child event can arrive for a root whose children are gone, or for a root that is gone: MongoDB rejects array filters
/// against a path the document does not hold, so such a write must be a no-op rather than a failed partition.
/// </summary>
/// <param name="ctx">The context.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_document_lacks_what_the_change_is_positioned_in(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        readonly string _databaseName = MongoDBSpecDatabaseNames.New();
        IMongoClient _client = default!;
        IMongoCollection<BsonDocument> _collection = default!;
        Sink _sink = default!;

        public List<FailedPartition> Failures { get; } = [];
        public BsonDocument WithoutArray = default!;
        public BsonDocument WithoutArrayInBulk = default!;
        public bool MissingRootWasCreated;

        public async Task InitializeAsync()
        {
            _client = new MongoClient(fixture.ConnectionString);
            var database = _client.GetDatabase(_databaseName);
            var schema = await JsonSchema.FromJsonAsync("""
                {"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"}}}}}}
                """);
            var readModel = new ReadModelDefinition("tree-model", "TreeModel", $"tree_{Guid.NewGuid():N}", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
            var formats = new TypeFormats();
            var expando = new ExpandoObjectConverter(formats);
            var collections = new SinkCollections(readModel, database);
            var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
            _sink = new Sink(readModel, converter, collections, new ChangesetConverter(readModel, converter, collections, expando), expando, Substitute.For<IReadModelChangeStreams>());
            _collection = collections.GetCollection();

            await _collection.InsertManyAsync(
            [
                new BsonDocument { { "_id", "without-array" }, { "__initialized", false } },
                new BsonDocument { { "_id", "without-array-in-bulk" }, { "__initialized", false } }
            ]);

            Failures.AddRange(await _sink.ApplyChanges(Key("without-array"), ItemNameChanged(), 1UL));
            Failures.AddRange(await _sink.ApplyChanges(Key("missing-root"), ItemNameChanged(), 1UL));
            await _sink.BeginBulk();
            Failures.AddRange(await _sink.ApplyChanges(Key("without-array-in-bulk"), ItemNameChanged(), 1UL));
            Failures.AddRange(await _sink.EndBulk());

            WithoutArray = await Find("without-array");
            WithoutArrayInBulk = await Find("without-array-in-bulk");
            MissingRootWasCreated = await Exists("missing-root");
        }

        public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

        static Key Key(string id) => new(id, ArrayIndexers.NoIndexers);

        static IChangeset<AppendedEvent, ExpandoObject> ItemNameChanged()
        {
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.InitialState.Returns(new ExpandoObject());
            changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(),
            [
                new PropertyDifference("[items].name", null, "Renamed", new ArrayIndexers([new ArrayIndexer("[items]", "id", "removed-item")]))
            ])]);
            return changeset;
        }

        async Task<BsonDocument> Find(string id) => await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();

        async Task<bool> Exists(string id) => await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).AnyAsync();
    }

    [Fact] void should_not_fail_any_partition() => ctx.Failures.ShouldBeEmpty();
    [Fact] void should_leave_a_document_without_the_array_without_it() => ctx.WithoutArray.Contains("items").ShouldBeFalse();
    [Fact] void should_leave_a_document_without_the_array_without_it_in_a_bulk_window() => ctx.WithoutArrayInBulk.Contains("items").ShouldBeFalse();
    [Fact] void should_not_create_a_missing_root_for_a_positioned_change() => ctx.MissingRootWasCreated.ShouldBeFalse();
}
