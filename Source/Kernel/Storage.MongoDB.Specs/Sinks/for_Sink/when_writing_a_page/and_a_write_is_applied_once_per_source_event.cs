// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page;

/// <summary>
/// A write applied once per source event guards each of its writes by its own applied event marker, so it is written on its
/// own rather than as part of the ordered page write.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> supplying the database.</param>
[Collection(MongoDBCollection.Name)]
public class and_a_write_is_applied_once_per_source_event(MongoDBFixture fixture) : given.a_page_sink(fixture)
{
    FailedPartition[] _failures;

    async Task Because() =>
        _failures = [.. await Pages.ApplyPage([Write("first", 1) with { Mode = SinkWriteMode.OncePerSourceEvent }, Write("second", 2)])];

    [Fact] void should_report_no_failures() => _failures.ShouldBeEmpty();
    [Fact] void should_write_it_on_its_own() => _directWrites.ShouldEqual(1);
    [Fact] void should_write_the_rest_of_the_page_in_one_bulk_write() => _bulkWrites.ShouldEqual(1);
    [Fact] async Task should_write_the_instance() => (await Document("first"))!["count"].AsInt32.ShouldEqual(1);
}
