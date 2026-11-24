// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_verifying_live_start;

public class and_a_gap_is_reported_to_metrics : given.a_queue_reporting_its_metrics
{
    async Task Establish()
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(new[] { EventAt(102) });
        cursor.MoveNext().Returns(true, false);
        var events = _storage.GetEventStore(EventStore).GetNamespace(Namespace).GetEventSequence(_eventSequence);
        events.GetRange(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSourceId?>(),
                Arg.Any<IEnumerable<EventType>?>(),
                Arg.Any<IEnumerable<Tag>?>(),
                Arg.Any<EventSourceType?>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<CancellationToken>())
            .Returns(cursor);
        _observer.GetState().Returns(new ObserverState { NextEventSequenceNumber = 102UL, RunningState = ObserverRunningState.Active });

        // The base seeds the observer as dormant; subscribing makes it live, so the batch goes through the live-start check.
        await _queue.Subscribe(_observerKey, [_eventType]);
    }

    async Task Because()
    {
        await _queue.Enqueue([EventAt(105)]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_report_a_live_delivery_gap() => Recorded(WellKnownMetricNames.AppendedEventsQueueObserverSpills, AppendedEventsQueueSpillReasons.LiveDeliveryGap).ShouldEqual(1);
    [Fact] void should_not_report_backlog_pressure() => Recorded(WellKnownMetricNames.AppendedEventsQueueObserverSpills, AppendedEventsQueueSpillReasons.LaggingBacklog).ShouldEqual(0);
}
