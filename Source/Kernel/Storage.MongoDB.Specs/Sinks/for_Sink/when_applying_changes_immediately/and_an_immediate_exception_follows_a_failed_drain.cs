// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_an_immediate_exception_follows_a_failed_drain(MongoDBFixture fixture) : given.a_controlled_mongo_sink(fixture)
{
    Exception? _immediateError;
    FailedPartition[] _remainingFailures;

    async Task Establish()
    {
        await RejectDuplicateCounts();
        await _sink.ApplyChanges(_queuedKey, Changes(1), 1UL);
        _directWriteError = _unavailable;
    }

    async Task Because()
    {
        _immediateError = await Catch.Exception(async () => await _sink.ApplyChangesImmediately(_futureKey, Changes(2), 2UL, SinkWriteMode.Always));
        _remainingFailures = (await _sink.EndBulk()).ToArray();
    }

    [Fact] void should_propagate_the_unknown_immediate_outcome() => _immediateError.ShouldEqual(_unavailable);
    [Fact] void should_keep_the_drained_failure_reportable() => _remainingFailures.Single().EventSourceId.ShouldEqual(_queuedKey);
    [Fact] void should_keep_the_original_failed_sequence_number() => _remainingFailures.Single().EventSequenceNumber.Value.ShouldEqual(1UL);
}
