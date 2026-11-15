// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page;

/// <summary>
/// A page redelivered after a later one was written must not move an instance back: the guarded write of an older
/// event leaves the document as it is.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> supplying the database.</param>
[Collection(MongoDBCollection.Name)]
public class and_a_guarded_write_is_redelivered(MongoDBFixture fixture) : given.a_page_sink(fixture)
{
    const long Watermark = 5;

    FailedPartition[] _failures;
    BsonDocument? _redelivered;
    BsonDocument? _other;

    async Task Establish() =>
        await _collection.InsertOneAsync(new BsonDocument { { "_id", "first" }, { "count", 5 }, { WellKnownProperties.LastHandledEventSequenceNumber, Watermark } });

    async Task Because()
    {
        _failures = [.. await Pages.ApplyPage([Guarded("first", 3), Write("second", 2)])];
        _redelivered = await Document("first");
        _other = await Document("second");
    }

    [Fact] void should_report_no_failures() => _failures.ShouldBeEmpty();
    [Fact] void should_leave_the_state_of_the_instance_unchanged() => _redelivered!["count"].AsInt32.ShouldEqual(5);
    [Fact] void should_leave_the_watermark_unchanged() => _redelivered![WellKnownProperties.LastHandledEventSequenceNumber].ToInt64().ShouldEqual(Watermark);
    [Fact] void should_write_the_rest_of_the_page() => _other!["count"].AsInt32.ShouldEqual(2);
}
