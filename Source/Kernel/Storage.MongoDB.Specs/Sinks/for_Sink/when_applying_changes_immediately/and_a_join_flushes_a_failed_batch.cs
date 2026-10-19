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
/// The flush before a join can drop other writers' operations. Their partitions are returned with the join's write,
/// so they are recorded as failed instead of being discarded.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> the specification runs against.</param>
[Collection(MongoDBCollection.Name)]
public class and_a_join_flushes_a_failed_batch(MongoDBFixture fixture) : given.a_controlled_mongo_sink(fixture)
{
    FailedPartition[] _joinFailures;
    FailedPartition[] _remainingFailures;

    async Task Establish()
    {
        _firstBatchError = _unavailable;
        await _sink.ApplyChanges(_queuedKey, Changes(1), 1UL);
    }

    async Task Because()
    {
        _joinFailures = (await _sink.ApplyChanges(new Key(1, ArrayIndexers.NoIndexers), JoinChanges(), 2UL)).ToArray();
        _remainingFailures = (await _sink.EndBulk()).ToArray();
    }

    [Fact] void should_return_the_dropped_partition() => _joinFailures.Single().EventSourceId.ShouldEqual(_queuedKey);
    [Fact] void should_return_the_dropped_sequence_number() => _joinFailures.Single().EventSequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_report_it_only_once() => _remainingFailures.ShouldBeEmpty();

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
