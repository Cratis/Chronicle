// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_reading_a_protected_parent_in_a_bulk_window : given.a_bulk_child_removal_from_all
{
    readonly Key _key = new("parent", ArrayIndexers.NoIndexers);
    ExpandoObject _stored;
    ExpandoObject _result;
    bool _flushedBeforeReading;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        var readModel = new ReadModelDefinition(
            "Parents",
            "parents",
            "Parents",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(_collection);
        var expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _sink = new Sink(readModel, _converter, collections, _changesetConverter, expandoObjectConverter, Substitute.For<IReadModelChangeStreams>());
        await _sink.BeginBulk();
        await Cache(_key, Document("name", "Ada Lovelace"));

        _stored = Document("name", "protected-name");
        ((IDictionary<string, object?>)_stored)[WellKnownProperties.Subject] = "erased-owner";
        var document = new BsonDocument("name", "protected-name");
        expandoObjectConverter.ToExpandoObject(document, schema).Returns(_stored);
        var cursor = CursorFor(document);
        _collection.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => new BulkWriteResult<BsonDocument>.Acknowledged(1, 1, 0, 1, 0, call.Arg<IEnumerable<WriteModel<BsonDocument>>>().ToArray(), []));
        _collection.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _flushedBeforeReading = _collection.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(_collection.BulkWriteAsync));
                return cursor;
            });
    }

    static IAsyncCursor<BsonDocument> CursorFor(BsonDocument document)
    {
        var cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
        cursor.Current.Returns([document]);
        return cursor;
    }

    async Task Because() => _result = await _sink.FindOrDefault(_key);

    [Fact] void should_read_the_protected_document_instead_of_the_plaintext_fold() => _result.ShouldEqual(_stored);
    [Fact] void should_settle_the_buffered_write_before_reading() => _flushedBeforeReading.ShouldBeTrue();
    [Fact] void should_preserve_the_stored_subject() => ((IDictionary<string, object?>)_result)[WellKnownProperties.Subject].ShouldEqual("erased-owner");
}
