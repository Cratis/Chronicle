// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_a_faulted_threshold_precedes_end_bulk(MongoDBFixture fixture) : given.a_faulted_threshold_sink(fixture)
{
    Exception? _endError;

    async Task Establish() => await _sink.ApplyChanges(_futureKey, Changes(2001), 2001UL);

    async Task Because() => _endError = await Catch.Exception(_sink.EndBulk);

    [Fact] void should_not_rethrow_the_reported_exception_without_futures() => _endError.ShouldBeNull();
    [Fact] async Task should_write_the_final_batch_without_futures() => (await Document(_futureKey))!["count"].AsInt32.ShouldEqual(2001);
}
