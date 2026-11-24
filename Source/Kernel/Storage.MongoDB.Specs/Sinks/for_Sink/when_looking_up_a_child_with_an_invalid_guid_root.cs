// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_looking_up_a_child_with_an_invalid_guid_root : given.a_sink_observing_a_container
{
    ILogger<MongoDBConverter> _converterLogger;
    bool _found;

    void Establish()
    {
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromType<GuidKeyedReadModel>();
        _converterLogger = Substitute.For<ILogger<MongoDBConverter>>();
        _converterLogger.IsEnabled(LogLevel.Warning).Returns(true);
        _converter = new MongoDBConverter(_expandoObjectConverter, new TypeFormats(), _readModel, _converterLogger);
        _converterLogger.ClearReceivedCalls();
        _sink = new Sink(_readModel, _converter, _collections, _changesetConverter, _expandoObjectConverter, _changeStreams);
    }

    async Task Because()
    {
        var root = new Key("separate-child-stream", ArrayIndexers.NoIndexers);
        _found = (await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "child", root)).HasValue;
        await _sink.BeginBulk();
        _found |= (await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "child", root)).HasValue;
    }

    [Fact] void should_return_none_for_both_normal_and_bulk_lookups() => _found.ShouldBeFalse();
    [Fact] void should_not_log_key_conversion_warnings() => _converterLogger.ReceivedCalls().ShouldBeEmpty();
    [Fact]
    void should_not_query_an_invalid_root_identifier() => _collection.DidNotReceive().FindAsync(
        Arg.Any<FilterDefinition<BsonDocument>>(),
        Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
        Arg.Any<CancellationToken>());
}
