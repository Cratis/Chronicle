// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.given;

public class an_observer_waiting_for_event_lookup_retry : an_observer_behind_on_a_relevant_event
{
    async Task Establish()
    {
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.StorageError);
        await _observer.TransitionTo<Routing>();
        _eventSequence.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    protected int LookupCount() => _eventSequence.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IEventSequence.GetNextSequenceNumberGreaterOrEqualTo));
}
