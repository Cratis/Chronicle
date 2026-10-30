// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_one_document_matches : given.a_composite_key_lookup
{
    void Establish() => _documents = [new BsonDocument("_id", _id)];
    async Task Because() => _result = await Lookup();
    [Fact] void should_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] void should_return_the_converted_instance() => _result.Instance.ShouldNotBeNull();
    [Fact] void should_bound_the_candidates() => _collection.Received(1).FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Is<FindOptions<BsonDocument, BsonDocument>>(_ => _.Limit == 2), Arg.Any<CancellationToken>());
}
