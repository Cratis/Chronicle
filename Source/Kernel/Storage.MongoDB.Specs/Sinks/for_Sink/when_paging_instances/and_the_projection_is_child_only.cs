// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ReadModelInstanceVisibility = Cratis.Chronicle.Storage.Sinks.ReadModelInstanceVisibility;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_paging_instances;

public class and_the_projection_is_child_only : given.a_sink_with_indexes
{
    readonly BsonDocument _expected = new();

    BsonDocument _pageFilter;
    BsonDocument _countFilter;

    static BsonDocument Render(FilterDefinition<BsonDocument> filter) => filter.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry));

    void Establish()
    {
        _collections.GetCollection(Arg.Any<string>()).Returns(_collection);
        _collection.CountDocumentsAsync(
            Arg.Do<FilterDefinition<BsonDocument>>(filter => _countFilter = Render(filter)),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>()).Returns(0L);
        _collection.FindAsync(
            Arg.Do<FilterDefinition<BsonDocument>>(filter => _pageFilter = Render(filter)),
            Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
            Arg.Any<CancellationToken>()).Returns(_indexCursor);
        _indexCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);
    }

    Task Because() => _sink.GetInstances(null, 0, 50, ReadModelInstanceVisibility.IncludeUninitialized);

    [Fact] void should_apply_no_marker_filter_for_paging() => _pageFilter.ShouldEqual(_expected);
    [Fact] void should_use_the_same_filter_for_counting() => _countFilter.ShouldEqual(_expected);
    [Fact] void should_not_use_an_expression() => _pageFilter.ToJson().Contains("$expr", StringComparison.Ordinal).ShouldBeFalse();
}
