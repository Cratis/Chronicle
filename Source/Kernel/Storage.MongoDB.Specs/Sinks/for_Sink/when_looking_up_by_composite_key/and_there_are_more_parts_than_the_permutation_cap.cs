// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_there_are_more_parts_than_the_permutation_cap : given.a_composite_key_lookup
{
    BsonDocument _filter;
    void Establish()
    {
        _id = new BsonDocument(Enumerable.Range(0, 6).Select(_ => new BsonElement($"part{_}", _)));
        _collection.FindAsync(Arg.Do<FilterDefinition<BsonDocument>>(_ => _filter = _.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry))), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>());
    }
    async Task Because() => _result = await Lookup();
    [Fact] void should_not_use_permutations() => _filter.ToJson().Contains("$in", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_guard_the_exact_part_count() => _filter.ToJson().Contains("$objectToArray", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_warn_about_the_collection_scan() => _logger.Entries.Any(_ => _.Level == LogLevel.Warning && Equals(_.Values["PartCount"], 6)).ShouldBeTrue();
}
