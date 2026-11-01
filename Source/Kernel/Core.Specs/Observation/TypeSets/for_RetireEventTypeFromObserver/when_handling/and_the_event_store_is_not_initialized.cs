// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets.for_RetireEventTypeFromObserver.when_handling;

public class and_the_event_store_is_not_initialized : given.a_type_set_store
{
    RetireEventTypeFromObserverResult _result;

    async Task Because() => _result = await new RetireEventTypeFromObserver("store", null, "observer", "event-log", "event-type").Handle(_storage);

    [Fact] void should_answer_not_initialized() => _result.Outcome.ShouldEqual(ObserverTypeSetOperationOutcome.NotInitialized);
    [Fact] void should_not_save_observer_state() => _observers.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_mark_observers_initialized() => _eventStore.DidNotReceive().MarkObserversInitialized();
}
