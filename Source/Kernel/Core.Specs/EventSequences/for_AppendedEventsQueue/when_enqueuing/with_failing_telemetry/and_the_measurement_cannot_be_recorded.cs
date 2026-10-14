// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_failing_telemetry;

/// <summary>
/// Recording the new measurement fails. The queue contains the failure exactly as it does without it, and the warning is still logged.
/// </summary>
public class and_the_measurement_cannot_be_recorded : given.a_single_subscriber_with_queue_telemetry
{
    AppendedEvent _failingPartitionEvent;
    AppendedEvent _healthyPartitionEvent;
    AppendedEvent _laterEvent;
    EventSourceId _failingPartition;
    EventSourceId _healthyPartition;
    InvalidOperationException _handlingFailure;

    void Establish()
    {
        _failingPartition = Guid.NewGuid();
        _healthyPartition = Guid.NewGuid();
        _handlingFailure = new("Handling failed");

        _failingPartitionEvent = EventFor(_failingPartition, 0UL);
        _healthyPartitionEvent = EventFor(_healthyPartition, 1UL);
        _laterEvent = EventFor(_healthyPartition, 2UL);

        _observer
            .When(_ => _.Handle(Arg.Is<Key>(key => key == (Key)_failingPartition), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(_ => throw _handlingFailure);
        _recordedMeasurements.Throws = true;
    }

    async Task Because()
    {
        await _queue.Enqueue([_failingPartitionEvent, _healthyPartitionEvent]);
        await _queue.AwaitQueueDepletion();
        await _queue.Enqueue([_laterEvent]);
        await _queue.AwaitQueueDepletion();
    }

    AppendedEvent EventFor(EventSourceId partition, ulong sequenceNumber) => AppendedEvent.Empty() with
    {
        Context = EventContext.Empty with
        {
            EventType = _eventType,
            EventSourceId = partition,
            SequenceNumber = sequenceNumber
        }
    };

    [Fact] void should_still_deliver_the_healthy_partition() => _handledEventsPerPartition.ContainsKey(_healthyPartition).ShouldBeTrue();
    [Fact] void should_keep_draining_the_queue() => _handledEventsPerPartition[_healthyPartition].Count.ShouldEqual(2);
    [Fact] void should_log_the_original_failure_and_not_the_telemetry_failure() => _recordedLogs.Exceptions.ShouldContain(_handlingFailure);
    [Fact] void should_not_log_the_telemetry_failure() => _recordedLogs.Exceptions.Count(_ => _.Message == "The logger failed" || _.Message == "The measurement listener failed").ShouldEqual(0);
    [Fact] void should_still_warn() => _recordedLogs.SuspectedUndeliveredWarnings.Count().ShouldEqual(1);
}
