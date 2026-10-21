// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_immediately;

/// <summary>
/// When the join's own write throws after its flush dropped a batch, the dropped partitions stay reportable and are
/// returned when bulk mode ends.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> the specification runs against.</param>
[Collection(MongoDBCollection.Name)]
public class and_a_join_write_fails_after_a_failed_flush(MongoDBFixture fixture) : given.a_controlled_mongo_sink(fixture)
{
    Exception? _joinError;
    FailedPartition[] _remainingFailures;

    async Task Establish()
    {
        _firstBatchError = _unavailable;
        _joinWriteError = _unavailable;
        await _sink.ApplyChanges(_queuedKey, Changes(1), 1UL);
    }

    async Task Because()
    {
        _joinError = await Catch.Exception(async () => await _sink.ApplyChanges(new Key(1, ArrayIndexers.NoIndexers), JoinChanges(), 2UL));
        _remainingFailures = (await _sink.EndBulk()).ToArray();
    }

    [Fact] void should_propagate_the_join_failure() => _joinError.ShouldEqual(_unavailable);
    [Fact] void should_keep_the_dropped_partition_reportable() => _remainingFailures.Single().EventSourceId.ShouldEqual(_queuedKey);
    [Fact] void should_keep_the_dropped_sequence_number() => _remainingFailures.Single().EventSequenceNumber.Value.ShouldEqual(1UL);

    static Changeset<AppendedEvent, ExpandoObject> JoinChanges()
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.Add(new Joined(
            new ExpandoObject(),
            1,
            "count",
            ArrayIndexers.NoIndexers,
            [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("info.name", null, "Joined")])]));
        return changeset;
    }
}
