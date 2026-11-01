// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets.for_RetireEventTypeFromObserver.when_handling;

public class and_the_event_store_is_initialized : given.a_type_set_store
{
    RetireEventTypeFromObserverResult _result;

    void Establish() => _eventStore.AreObserversInitialized().Returns(true);
    async Task Because() => _result = await new RetireEventTypeFromObserver("store", "namespace", "observer", "event-log", "event-type").Handle(_storage);

    [Fact] void should_answer_unavailable() => _result.Outcome.ShouldEqual(ObserverTypeSetOperationOutcome.Unavailable);
    [Fact] void should_not_save_observer_state() => _observers.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_mark_observers_initialized() => _eventStore.DidNotReceive().MarkObserversInitialized();
}
