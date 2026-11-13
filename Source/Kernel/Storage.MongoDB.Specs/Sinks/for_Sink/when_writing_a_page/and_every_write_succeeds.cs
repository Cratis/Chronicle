// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page;

[Collection(MongoDBCollection.Name)]
public class and_every_write_succeeds(MongoDBFixture fixture) : given.a_page_sink(fixture)
{
    FailedPartition[] _failures;
    int? _first;
    int? _second;
    int? _third;

    async Task Because()
    {
        _failures = [.. await Pages.ApplyPage([Write("first", 1), Write("second", 2), Write("third", 3)])];

        // Read straight from the collection, not through the sink: the writes must be durable when the page returns.
        _first = (await Document("first"))?["count"].AsInt32;
        _second = (await Document("second"))?["count"].AsInt32;
        _third = (await Document("third"))?["count"].AsInt32;
    }

    [Fact] void should_report_no_failures() => _failures.ShouldBeEmpty();
    [Fact] void should_write_the_page_in_one_bulk_write() => _bulkWrites.ShouldEqual(1);
    [Fact] void should_not_write_any_instance_on_its_own() => _directWrites.ShouldEqual(0);
    [Fact] void should_have_written_the_first_instance() => _first.ShouldEqual(1);
    [Fact] void should_have_written_the_second_instance() => _second.ShouldEqual(2);
    [Fact] void should_have_written_the_third_instance() => _third.ShouldEqual(3);
    [Fact] void should_not_open_a_bulk_window() => _sink.IsInBulkMode.ShouldBeFalse();
}
