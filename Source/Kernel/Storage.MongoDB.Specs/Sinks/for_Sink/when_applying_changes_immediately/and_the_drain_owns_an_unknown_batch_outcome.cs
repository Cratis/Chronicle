// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_the_drain_owns_an_unknown_batch_outcome(MongoDBFixture fixture) : given.a_controlled_mongo_sink(fixture)
{
    FailedPartition[] _failures;
    FailedPartition[] _laterFailures;

    async Task Establish()
    {
        _firstBatchError = _unavailable;
        await _sink.ApplyChanges(_queuedKey, Changes(1), 1UL);
    }

    async Task Because()
    {
        _failures = (await _sink.ApplyChangesImmediately(_futureKey, Changes(2), 2UL, SinkWriteMode.Always)).ToArray();
        _laterFailures = (await _sink.EndBulk()).ToArray();
    }

    [Fact] void should_report_the_affected_key_as_failed() => _failures.Single().EventSourceId.ShouldEqual(_queuedKey);
    [Fact] void should_describe_the_unknown_batch_outcome() => _failures.Single().Reason.ShouldContain("outcome unknown");
    [Fact] void should_not_report_the_outcome_again() => _laterFailures.ShouldBeEmpty();
    [Fact] async Task should_commit_the_independent_immediate_write() => (await Document(_futureKey))!["count"].AsInt32.ShouldEqual(2);
}
