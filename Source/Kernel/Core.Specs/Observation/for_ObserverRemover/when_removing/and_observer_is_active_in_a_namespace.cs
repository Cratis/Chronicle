// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

/// <summary>
/// An observer that is running in any namespace is a live observer, whatever the namespace the operator happens to be
/// looking at. The refusal has to name the namespace that blocked it, or there is nothing to act on.
/// </summary>
public class and_observer_is_active_in_a_namespace : given.all_dependencies
{
    ObserverRemovalResult _result;

    void Establish() => ObserverIs(_observerInSecondNamespace, ObserverRunningState.Active, false);

    async Task Because() => _result = await Remove();

    [Fact] void should_refuse_because_the_observer_is_active() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.ObserverActive);
    [Fact] void should_name_the_namespace_that_blocked_it() => _result.BlockingNamespace.ShouldEqual(_secondNamespace);
    [Fact] async Task should_not_remove_the_observer_in_the_namespace_that_was_idle() => await _observerInFirstNamespace.DidNotReceive().Remove();
    [Fact] async Task should_not_delete_the_definition() => await _observerDefinitions.DidNotReceive().Delete(Arg.Any<ObserverId>());
    [Fact] async Task should_not_delete_any_observer_state() => await _firstNamespaceStorage.Observers.DidNotReceive().Delete(Arg.Any<ObserverId>());
}
