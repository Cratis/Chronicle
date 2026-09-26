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
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_repairing_a_failed_bulk_write;

public class and_the_operation_is_not_key_scoped : Specification
{
    Sink _sink;
    IMongoCollection<BsonDocument> _collection;
    IEnumerable<FailedPartition> _failedPartitions;
    int _keyConversions;

    void Establish()
    {
        var definition = new ReadModelDefinition(
            "probe",
            "Probe",
            "Probe",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(Arg.Any<Key>()).Returns(_ =>
        {
            _keyConversions++;
            throw new FormatException("Unrecognized Guid format");
        });
        var collections = Substitute.For<ISinkCollections>();
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collections.GetCollection().Returns(_collection);
        var changesetConverter = Substitute.For<IChangesetConverter>();
        changesetConverter.ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>())
            .Returns(new UpdateDefinitionAndArrayFilters(Builders<BsonDocument>.Update.Set("outer.info.name", "ciphertext"), [], true));
        _sink = new Sink(definition, converter, collections, changesetConverter, Substitute.For<IExpandoObjectConverter>());
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BulkWriteResult<BsonDocument>>(given.an_ordered_bulk_with_a_null_parent_failure.CreateBulkFailure(0)));
    }

    async Task Because()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new Joined(new ExpandoObject(), "not-a-guid", new PropertyPath("JoinProperty"), ArrayIndexers.NoIndexers, [])]);
        changeset.HasJoined().Returns(true);
        await _sink.BeginBulk();
        _failedPartitions = await _sink.ApplyChanges(new Key("not-a-guid", ArrayIndexers.NoIndexers), changeset, 42UL);
    }

    [Fact] void should_report_the_failed_partition_without_converting_the_join_value_to_an_id() => _failedPartitions.Single().EventSourceId.Value.ShouldEqual("not-a-guid");
    [Fact] void should_not_attempt_a_key_scoped_repair() => _collection.DidNotReceive().UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_convert_the_join_value() => _keyConversions.ShouldEqual(0);
}
