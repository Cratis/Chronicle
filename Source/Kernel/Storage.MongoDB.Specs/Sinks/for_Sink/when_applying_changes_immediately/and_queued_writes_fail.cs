// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_queued_writes_fail(MongoDBFixture fixture) : given.a_bulk_sink(fixture)
{
    FailedPartition[] _failures;
    FailedPartition[] _remainingFailures;
    ExpandoObject? _failedState;
    bool _futureWasWritten;

    async Task Establish()
    {
        await RejectDuplicateCounts();
        await _sink.ApplyChanges(_queuedKey, Changes(1), 1UL);
    }

    async Task Because()
    {
        _failures = (await _sink.ApplyChangesImmediately(_futureKey, Changes(2), 2UL, SinkWriteMode.Always)).ToArray();
        _futureWasWritten = (await Document(_futureKey))?["count"].AsInt32 == 2;
        _failedState = await _sink.FindOrDefault(_queuedKey);
        await _sink.ApplyChanges(_queuedKey, Changes(1), 3UL);
        _remainingFailures = (await _sink.EndBulk()).ToArray();
    }

    [Fact] void should_return_the_drained_failure() => _failures.Single().EventSourceId.ShouldEqual(_queuedKey);
    [Fact] void should_keep_the_failed_event_sequence_number() => _failures.Single().EventSequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_commit_the_future_before_returning() => _futureWasWritten.ShouldBeTrue();
    [Fact] void should_not_serve_the_failed_root_from_cache() => _failedState.ShouldBeNull();
    [Fact] void should_report_remaining_failures_at_end_bulk() => _remainingFailures.Single().EventSequenceNumber.Value.ShouldEqual(3UL);
}
