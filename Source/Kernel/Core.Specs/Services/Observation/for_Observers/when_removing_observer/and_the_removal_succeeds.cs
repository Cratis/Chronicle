// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;

using KernelObserverRemovalResult = Cratis.Chronicle.Concepts.Observation.ObserverRemovalResult;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_removing_observer;

public class and_the_removal_succeeds : given.all_dependencies
{
    RemoveObserverResponse _result;

    void Establish() =>
        _observerRemover
            .Remove(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.Observation.ObserverId>(), Arg.Any<EventSequenceId>())
            .Returns(KernelObserverRemovalResult.Removed);

    async Task Because() => _result = await _observers.RemoveObserver(new RemoveObserver
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = EventSequenceId.Log.Value
    });

    [Fact] void should_report_the_observer_as_removed() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] void should_not_name_a_blocking_namespace() => _result.BlockingNamespace.ShouldBeEmpty();
}
