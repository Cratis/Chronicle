// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

/// <summary>
/// The stored running state and the subscription answer different questions. An observer can read as Disconnected -
/// which on its own would let the removal through - while a client has just resubscribed to it. The subscription is
/// what says the declaring code still exists, so it is checked in its own right rather than inferred from the state.
/// </summary>
public class and_a_client_is_still_subscribed : given.all_dependencies
{
    ObserverRemovalResult _result;

    void Establish() => ObserverIs(_observerInFirstNamespace, ObserverRunningState.Disconnected, true);

    async Task Because() => _result = await Remove();

    [Fact] void should_refuse_because_a_client_is_subscribed() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.ObserverSubscribed);
    [Fact] void should_name_the_namespace_that_blocked_it() => _result.BlockingNamespace.ShouldEqual(_firstNamespace);
    [Fact] async Task should_not_remove_the_observer_grain() => await _observerInFirstNamespace.DidNotReceive().Remove();
    [Fact] async Task should_not_delete_the_definition() => await _observerDefinitions.DidNotReceive().Delete(Arg.Any<ObserverId>());
}
