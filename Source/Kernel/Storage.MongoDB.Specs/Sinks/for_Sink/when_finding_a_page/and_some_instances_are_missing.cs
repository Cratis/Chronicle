// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_page;

[Collection(MongoDBCollection.Name)]
public class and_some_instances_are_missing(MongoDBFixture fixture) : when_writing_a_page.given.a_page_sink(fixture)
{
    IReadOnlyList<ExpandoObject?> _found;

    async Task Establish() => await _collection.InsertManyAsync(
    [
        new BsonDocument { { "_id", "first" }, { "count", 1 } },
        new BsonDocument { { "_id", "second" }, { "count", 2 } }
    ]);

    async Task Because() => _found = await Pages.FindPage([KeyFor("second"), KeyFor("missing"), KeyFor("first")]);

    [Fact] void should_read_the_page_in_one_read() => _reads.ShouldEqual(1);
    [Fact] void should_answer_in_the_order_asked() => ((IDictionary<string, object?>)_found[0]!)["count"].ShouldEqual(2);
    [Fact] void should_answer_null_for_a_missing_instance() => _found[1].ShouldBeNull();
    [Fact] void should_answer_the_last_instance() => ((IDictionary<string, object?>)_found[2]!)["count"].ShouldEqual(1);
    [Fact] void should_not_open_a_bulk_window() => _sink.IsInBulkMode.ShouldBeFalse();
}
