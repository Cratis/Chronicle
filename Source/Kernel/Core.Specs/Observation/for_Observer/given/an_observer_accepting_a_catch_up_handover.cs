// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_accepting_a_catch_up_handover : an_observer_with_controlled_persistence
{
    protected override ObserverState PersistedObserverState => _writes.PersistedState;

    async Task Establish()
    {
        _writes.State = _writes.State with { NextEventSequenceNumber = 9UL, CatchingUpPartitions = new HashSet<Concepts.Keys.Key> { "A", "B" } };
        await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(EventType.Unknown, 10UL)]);
        _jobsManager.ClearReceivedCalls();
    }
}
