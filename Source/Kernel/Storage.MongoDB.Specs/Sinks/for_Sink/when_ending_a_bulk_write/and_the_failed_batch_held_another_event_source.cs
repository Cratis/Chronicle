// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

/// <summary>
/// A failed partition is the event source of the event whose write was lost, not the read model it was written to;
/// only that event source can be retried.
/// </summary>
public class and_the_failed_batch_held_another_event_source : given.a_sink_with_gated_bulk_writes
{
    readonly EventSourceId _eventSourceId = "the-event-source";
    FailedPartition[] _failures;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _throwFirstBatch = true;
        await _sink.BeginBulk();
        await _sink.ApplyChanges(_firstKey, ChangesFrom(_eventSourceId), 1UL);
    }

    async Task Because() => _failures = (await _sink.EndBulk()).ToArray();

    [Fact] void should_report_the_event_source_as_the_failed_partition() => _failures.Single().EventSourceId.Value.ShouldEqual(_eventSourceId.Value);

    static Changeset<AppendedEvent, ExpandoObject> ChangesFrom(EventSourceId eventSourceId)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = 1;
        var incoming = new AppendedEvent(EventContext.EmptyWithEventSourceId(eventSourceId), new ExpandoObject());
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, new ExpandoObject());
        changeset.ReplaceState(state, [new PropertyDifference("count", null, 1)]);
        return changeset;
    }
}
