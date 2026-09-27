// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_paging_instances;

public class and_reading_a_page : a_sink_with_indexes
{
    FindOptions<BsonDocument, BsonDocument> _options;

    void Establish()
    {
        _collection.CountDocumentsAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(0L);
        _collection.FindAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Do<FindOptions<BsonDocument, BsonDocument>>(options => _options = options),
            Arg.Any<CancellationToken>()).Returns(_indexCursor);
        _indexCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
    }

    async Task Because() => await _sink.GetInstances(skip: 1, take: 2);

    [Fact] void should_sort_by_the_document_key() =>
        _options.Sort.Render(new RenderArgs<BsonDocument>(global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry))
            .ShouldEqual(new BsonDocument("_id", 1));
}
