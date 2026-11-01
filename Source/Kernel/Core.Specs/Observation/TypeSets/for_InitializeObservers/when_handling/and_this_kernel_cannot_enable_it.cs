// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets.for_InitializeObservers.when_handling;

public class and_this_kernel_cannot_enable_it : given.a_type_set_store
{
    InitializeObserversResult _result;

    void Because() => _result = new InitializeObservers("store", true, true).Handle();

    [Fact] void should_answer_not_available() => _result.Outcome.ShouldEqual(ObserverTypeSetOperationOutcome.NotAvailable);
    [Fact] void should_list_no_observer_results() => _result.Observers.ShouldBeEmpty();
    [Fact] void should_list_no_reasons() => _result.Reasons.ShouldBeEmpty();
    [Fact] void should_not_mark_observers_initialized() => _eventStore.DidNotReceive().MarkObserversInitialized();
    [Fact] void should_not_access_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
}
