// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

[Collection(MongoDBCollection.Name)]
public class and_a_join_exception_follows_a_failed_drain(MongoDBFixture fixture) : given.a_controlled_mongo_sink(fixture)
{
    Task<IEnumerable<FailedPartition>> _queuedDuringDrain;
    Exception? _joinError;
    FailedPartition[] _remainingFailures;

    async Task Establish()
    {
        await RejectDuplicateCounts();
        await _sink.ApplyChanges(_futureKey, Changes(2), 1UL);
        _joinWriteError = _unavailable;
        _afterBatch = _ => _queuedDuringDrain = _sink.ApplyChanges(_queuedKey, Changes(1), 2UL);
    }

    async Task Because()
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.Add(new Joined(
            new ExpandoObject(),
            1,
            "count",
            ArrayIndexers.NoIndexers,
            [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("info.name", null, "Joined")])]));
        _joinError = await Catch.Exception(async () => await _sink.ApplyChangesImmediately(new Key(1, ArrayIndexers.NoIndexers), changeset, 3UL, SinkWriteMode.Always));
        await _queuedDuringDrain;
        _remainingFailures = (await _sink.EndBulk()).ToArray();
    }

    [Fact] void should_propagate_the_unknown_join_outcome() => _joinError.ShouldEqual(_unavailable);
    [Fact] void should_keep_the_pre_join_failure_reportable() => _remainingFailures.Single().EventSourceId.ShouldEqual(_queuedKey);
    [Fact] void should_keep_the_failed_pre_join_sequence_number() => _remainingFailures.Single().EventSequenceNumber.Value.ShouldEqual(2UL);
}
