// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;

using KernelObserverRemovalResult = Cratis.Chronicle.Concepts.Observation.ObserverRemovalResult;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_removing_observer;

public class and_the_refusal_is_store_wide : given.all_dependencies
{
    RemoveObserverResponse _result;

    void Establish() => _observerRemover
        .Remove(Arg.Any<EventStoreName>(), Arg.Any<Concepts.Observation.ObserverId>(), Arg.Any<EventSequenceId>())
        .Returns(KernelObserverRemovalResult.Subscribed(EventStoreNamespaceName.NotSet));

    async Task Because() => _result = await _observers.RemoveObserver(new RemoveObserver
    {
        EventStore = "some-event-store",
        ObserverId = "some-projection"
    });

    [Fact] void should_preserve_the_refusal() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.ObserverSubscribed);
    [Fact] void should_not_expose_the_namespace_sentinel() => _result.BlockingNamespace.ShouldEqual(string.Empty);
}
