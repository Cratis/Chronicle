// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page;

// A replay holds the window; a page written meanwhile joins it rather than writing past it.
[Collection(MongoDBCollection.Name)]
public class and_a_bulk_window_is_open(MongoDBFixture fixture) : given.a_page_sink(fixture)
{
    int _bulkWritesBeforeTheWindowEnded;
    bool _stillInBulkMode;

    async Task Establish() => await _sink.BeginBulk();

    async Task Because()
    {
        await Pages.ApplyPage([Write("first", 1), Write("second", 2)]);
        _bulkWritesBeforeTheWindowEnded = _bulkWrites;
        _stillInBulkMode = _sink.IsInBulkMode;
        await _sink.EndBulk();
    }

    [Fact] void should_buffer_the_writes_in_the_window() => _bulkWritesBeforeTheWindowEnded.ShouldEqual(0);
    [Fact] void should_leave_the_window_open() => _stillInBulkMode.ShouldBeTrue();
    [Fact] async Task should_write_them_when_the_window_ends() => (await Document("second"))!["count"].AsInt32.ShouldEqual(2);
}
