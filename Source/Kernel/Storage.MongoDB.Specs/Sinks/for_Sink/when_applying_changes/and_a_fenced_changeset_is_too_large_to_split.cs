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
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

public class and_a_fenced_changeset_is_too_large_to_split : Specification
{
    readonly Key _key = new("order-1", ArrayIndexers.NoIndexers);
    readonly RecordingLogger _logger = new();
    readonly ReadModelIdentifier _readModelIdentifier = "Orders";
    Sink _sink;
    Exception? _error;

    void Establish()
    {
        var readModel = new ReadModelDefinition(
            _readModelIdentifier,
            "orders",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new JsonSchema() },
            []);
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(collection);
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(Arg.Any<Key>()).Returns(new BsonString("order-1"));
        var changesetConverter = Substitute.For<IChangesetConverter>();
        changesetConverter.ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>())
            .Returns(new UpdateDefinitionAndArrayFilters(new BsonDocumentUpdateDefinition<BsonDocument>(new BsonDocument()), [], false));
        var writes = Substitute.For<ISinkWrites>();
        writes.IsInBatch.Returns(false);
        writes.MaxBatchSize.Returns(2);
        writes.InBatch(Arg.Any<Func<Task<IEnumerable<FailedPartition>>>>())
            .ThrowsAsync(new NonIdempotentWriteCannotBeSplit("bulk", ["$push"], 2));
        _sink = new Sink(readModel, converter, collections, changesetConverter, Substitute.For<IExpandoObjectConverter>(), Substitute.For<IReadModelChangeStreams>(), _logger, writes);
    }

    async Task Because() => _error = await Catch.Exception(() => _sink.ApplyChanges(_key, Changes(), 1UL));

    static Changeset<AppendedEvent, ExpandoObject> Changes()
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = 1;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.ReplaceState(state, [new PropertyDifference("count", null, 1)]);
        return changeset;
    }

    [Fact] void should_raise_the_named_exception() => _error.ShouldBeOfExactType<OversizedChangesetCannotBeSplit>();
    [Fact] void should_still_be_an_invalid_operation() => (_error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_carry_the_read_model() => ((OversizedChangesetCannotBeSplit)_error!).ReadModel.ShouldEqual(_readModelIdentifier);
    [Fact] void should_carry_the_key() => ((OversizedChangesetCannotBeSplit)_error!).Key.ShouldEqual(_key);
    [Fact] void should_use_the_key_as_partition() => ((OversizedChangesetCannotBeSplit)_error!).Partition.ShouldEqual(_key);
    [Fact] void should_keep_the_fencing_exception_inside() => _error!.InnerException.ShouldBeOfExactType<NonIdempotentWriteCannotBeSplit>();
    [Fact] void should_name_the_read_model() => _error!.Message.ShouldContain("Orders");
    [Fact] void should_name_the_key() => _error!.Message.ShouldContain("order-1");
    [Fact] void should_name_the_count() => _error!.Message.ShouldContain("more than 2");
    [Fact] void should_name_the_operator() => _error!.Message.ShouldContain("$push");
    [Fact] void should_name_the_setting() => _error!.Message.ShouldContain("ReadModels.MaxBatchSize");
    [Fact] void should_log_one_error() => _logger.Entries.Count(entry => entry.Level == LogLevel.Error).ShouldEqual(1);
    [Fact] void should_log_the_read_model() => _logger.Entries.Single(entry => entry.Level == LogLevel.Error).Values["ReadModel"]!.ToString().ShouldContain("Orders");
    [Fact] void should_log_the_key() => _logger.Entries.Single(entry => entry.Level == LogLevel.Error).Values["Key"]!.ToString().ShouldContain("order-1");
    [Fact] void should_log_the_partition() => _logger.Entries.Single(entry => entry.Level == LogLevel.Error).Values["Partition"]!.ToString().ShouldContain("order-1");
    [Fact] void should_log_the_cap() => _logger.Entries.Single(entry => entry.Level == LogLevel.Error).Values["MaxBatchSize"].ShouldEqual(2);
    [Fact] void should_log_the_operators() => _logger.Entries.Single(entry => entry.Level == LogLevel.Error).Values["Operations"].ShouldEqual("$push");
    [Fact] void should_not_attach_the_exception() => _logger.Entries.Single(entry => entry.Level == LogLevel.Error).HasException.ShouldBeFalse();
}
