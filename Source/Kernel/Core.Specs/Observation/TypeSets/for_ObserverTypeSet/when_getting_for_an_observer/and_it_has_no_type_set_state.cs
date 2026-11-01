// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.TypeSets.for_ObserverTypeSet.when_getting_for_an_observer;

public class and_it_has_no_type_set_state : given.a_type_set_store
{
    ObserverTypeSet _result;

    void Establish() => _observers.GetReadOnly(Arg.Any<ObserverId>()).Returns(ObserverState.Empty);
    async Task Because() => _result = await ObserverTypeSet.GetForObserver("store", "namespace", "observer", _storage);

    [Fact] void should_not_use_the_migrating_get() => _observers.DidNotReceive().Get(Arg.Any<ObserverId>());
    [Fact] void should_be_not_initialized() => _result.InitializationState.ShouldEqual(ObserverInitializationState.NotInitialized);
    [Fact] void should_have_no_members() => _result.Members.ShouldBeEmpty();
    [Fact] void should_have_no_pending_change() => _result.PendingChange.ShouldBeNull();
    [Fact] void should_have_no_registrations() => _result.Registrations.ShouldBeEmpty();
    [Fact] void should_have_no_retirement_results() => _result.RetirementResults.ShouldBeEmpty();
    [Fact] void should_have_no_refusal_reasons() => _result.RefusalReasons.ShouldBeEmpty();
    [Fact] void should_have_zero_revision() => _result.Revision.ShouldEqual(0L);
}
