// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;

using KernelObserverRemovalResult = Cratis.Chronicle.Concepts.Observation.ObserverRemovalResult;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_removing_observer;

/// <summary>
/// The refusal has to survive the trip across the wire intact, blocking namespace included. A caller told only that
/// the removal failed has no way of knowing which application to stop before trying again.
/// </summary>
public class and_the_removal_is_refused : given.all_dependencies
{
    RemoveObserverResponse _result;

    void Establish() =>
        _observerRemover
            .Remove(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.Observation.ObserverId>(), Arg.Any<EventSequenceId>())
            .Returns(KernelObserverRemovalResult.Active("the-busy-namespace"));

    async Task Because() => _result = await _observers.RemoveObserver(new RemoveObserver
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = EventSequenceId.Log.Value
    });

    [Fact] void should_report_the_observer_as_active() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.ObserverActive);
    [Fact] void should_name_the_namespace_that_blocked_it() => _result.BlockingNamespace.ShouldEqual("the-busy-namespace");
}
