// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_a_faulted_threshold_precedes_queued_writes(MongoDBFixture fixture) : given.a_faulted_threshold_sink(fixture)
{
    readonly Key _afterFault = new("after-fault", ArrayIndexers.NoIndexers);
    readonly Key _atEnd = new("at-end", ArrayIndexers.NoIndexers);
    Exception? _immediateError;
    Exception? _endError;
    bool _queuedWriteWasCommitted;
    bool _futureWasCommitted;

    async Task Establish() => await _sink.ApplyChanges(_afterFault, Changes(2001), 2001UL);

    async Task Because()
    {
        _immediateError = await Catch.Exception(async () => await _sink.ApplyChangesImmediately(_futureKey, Changes(2002), 2002UL, SinkWriteMode.Always));
        _queuedWriteWasCommitted = (await Document(_afterFault))?["count"].AsInt32 == 2001;
        _futureWasCommitted = (await Document(_futureKey))?["count"].AsInt32 == 2002;
        await _sink.ApplyChanges(_atEnd, Changes(2003), 2003UL);
        _endError = await Catch.Exception(_sink.EndBulk);
    }

    [Fact] void should_surface_the_threshold_exception_once() => _thresholdError.ShouldEqual(_unavailable);
    [Fact] void should_write_the_independent_queued_root_before_returning() => _queuedWriteWasCommitted.ShouldBeTrue();
    [Fact] void should_commit_the_immediate_write() => _futureWasCommitted.ShouldBeTrue();
    [Fact] void should_not_rethrow_a_reported_threshold_exception() => _immediateError.ShouldBeNull();
    [Fact] void should_not_poison_end_bulk() => _endError.ShouldBeNull();
    [Fact] async Task should_commit_the_remaining_queued_root() => (await Document(_atEnd))!["count"].AsInt32.ShouldEqual(2003);
}
