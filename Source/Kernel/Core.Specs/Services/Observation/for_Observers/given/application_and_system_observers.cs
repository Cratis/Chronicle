// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.given;

public class application_and_system_observers : all_dependencies
{
    protected const string SystemObserverId = "$system.patterns";
    protected const string ApplicationObserverId = "application-observer";
    protected IObserver _systemObserver;
    protected IObserver _applicationObserver;
    protected WaitForObserverCompletionRequest _request;

    void Establish()
    {
        _systemObserver = Substitute.For<IObserver>();
        _applicationObserver = Substitute.For<IObserver>();
        _systemObserver.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _applicationObserver.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _grainFactory.GetGrain<IObserver>(Arg.Is<string>(key => key.StartsWith(SystemObserverId, StringComparison.Ordinal))).Returns(_systemObserver);
        _grainFactory.GetGrain<IObserver>(Arg.Is<string>(key => key.StartsWith(ApplicationObserverId, StringComparison.Ordinal))).Returns(_applicationObserver);
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(SystemObserverId, [new EventType("a-recorded", 1)], Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Kernel, false),
            new ObserverDefinition(ApplicationObserverId, [new EventType("a-recorded", 1)], Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true)
        ]);
        _observerStateStorage.GetAll().Returns([]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
        _request = new()
        {
            EventStore = "event-store",
            Namespace = "second-namespace",
            EventSequenceId = Concepts.EventSequences.EventSequenceId.Log,
            FirstEventSequenceNumber = 0UL,
            TailEventSequenceNumber = 0UL,
            EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = "a-recorded", Generation = 1 }, SequenceNumber = 0UL }],
            TimeoutMilliseconds = 1
        };
    }
}
