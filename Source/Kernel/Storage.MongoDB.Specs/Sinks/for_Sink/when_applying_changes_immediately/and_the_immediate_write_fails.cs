// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_the_immediate_write_fails(MongoDBFixture fixture) : given.a_bulk_sink(fixture)
{
    FailedPartition[] _failures;

    async Task Establish()
    {
        await RejectDuplicateCounts();
        await _sink.ApplyChanges(_queuedKey, Changes(1), 1UL);
    }

    async Task Because() => _failures = (await _sink.ApplyChangesImmediately(_futureKey, Changes(1), 2UL, SinkWriteMode.Always)).ToArray();

    [Fact] void should_report_both_write_failures() => _failures.Select(failure => failure.EventSourceId).ShouldContainOnly(_queuedKey, _futureKey);
    [Fact] void should_include_the_immediate_write_error() => _failures.Single(failure => failure.EventSourceId == _futureKey).Reason.ShouldContain("11000");
    [Fact] async Task should_not_create_the_future_root() => (await Document(_futureKey)).ShouldBeNull();
}
