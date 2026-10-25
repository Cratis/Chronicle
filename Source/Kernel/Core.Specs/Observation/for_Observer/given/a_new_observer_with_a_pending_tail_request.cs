// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_new_observer_with_a_pending_tail_request : an_observer_under_a_mismatching_key
{
    async Task Establish()
    {
        _persistedStateStorage!.PersistedState = ObserverState.Empty;
        _stateStorage.State = ObserverState.Empty;
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(100UL));
        await _observer.StartAtTailWhenNew();
    }
}
