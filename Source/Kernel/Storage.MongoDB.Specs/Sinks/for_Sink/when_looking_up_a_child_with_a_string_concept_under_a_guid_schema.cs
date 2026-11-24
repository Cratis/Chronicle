// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_looking_up_a_child_with_a_string_concept_under_a_guid_schema : given.a_sink_observing_a_container
{
    const string RootId = "concept-root";
    ILogger<MongoDBConverter> _converterLogger;
    BsonDocument _filter;
    Key _result;
    bool _found;

    void Establish()
    {
        _primaryId = Guid.NewGuid();
        _documents = [new BsonDocument("_id", RootId)];
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromType<GuidKeyedReadModel>();
        _converterLogger = Substitute.For<ILogger<MongoDBConverter>>();
        _converterLogger.IsEnabled(LogLevel.Warning).Returns(true);
        _converter = new MongoDBConverter(_expandoObjectConverter, new TypeFormats(), _readModel, _converterLogger);
        _sink = new Sink(_readModel, _converter, _collections, _changesetConverter, _expandoObjectConverter, _changeStreams);
        _collection.When(collection => collection.FindAsync(
                Arg.Any<FilterDefinition<BsonDocument>>(),
                Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>()))
            .Do(call => _filter = call.Arg<FilterDefinition<BsonDocument>>().Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)));
        _converterLogger.ClearReceivedCalls();
    }

    async Task Because()
    {
        var root = new Key(new TagConcept(RootId), ArrayIndexers.NoIndexers);
        _found = (await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "shared", root)).TryGetValue(out _result);
    }

    [Fact] void should_find_the_child_inside_the_string_root() => _found.ShouldBeTrue();
    [Fact] void should_query_the_stored_string_identifier() => _filter["_id"].ShouldEqual(new BsonString(RootId));
    [Fact] void should_require_the_child_identifier() => _filter["children.childId"].AsString.ShouldEqual("shared");
    [Fact] void should_return_the_stored_identifier() => _result.Value.ShouldEqual(RootId);
    [Fact] void should_not_log_conversion_warnings() => _converterLogger.ReceivedCalls().ShouldBeEmpty();
}
