// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_handled_events;

/// <summary>
/// A catch-up step reports the events it handled for its partition while routing awaits. Routing returns the state it
/// was handed before the report, which must not take the partition's whole count out of the totals (#4972).
/// </summary>
public class and_the_observer_is_routing : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "partition";

    TaskCompletionSource<EventSequenceNumber> _tail;

    void Establish()
    {
        _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSequence.GetTailSequenceNumber().Returns(_ => _tail.Task);
    }

    async Task Because()
    {
        var routing = _observer.TransitionTo<Routing>();

        await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 100UL }, JobStepId.New(), 1UL);

        _tail.SetResult(EventSequenceNumber.Unavailable);
        await routing;
    }

    [Fact] void should_keep_the_reported_total() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)100UL);
    [Fact] void should_keep_the_reported_total_for_the_event_type() => _stateStorage.State.HandledEventCountPerEventType[event_type.Id].ShouldEqual((EventCount)100UL);
}
