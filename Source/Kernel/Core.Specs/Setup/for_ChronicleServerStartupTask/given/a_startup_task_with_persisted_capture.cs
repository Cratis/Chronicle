// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

public class a_startup_task_with_persisted_capture : a_startup_task
{
    protected IObserver _captureObserver;

    void Establish()
    {
        _captureObserver = Substitute.For<IObserver>();
        var captureKey = new ObserverKey(PatternCapture.ObserverIdentifier, _eventStore, _namespace, EventSequenceId.Log);
        _grainFactory.GetGrain<IObserver>(captureKey).Returns(_captureObserver);
        _captureObserver.Ensure().Returns(_ => Task.FromException(new TimeoutException()));
        _patternCapture.Subscribe(_eventStore, _namespace).Returns(_ => Task.FromException(new TimeoutException()));
        _observerStateStorage.GetAll().Returns([
            ObserverState.Empty with { Identifier = PatternCapture.ObserverIdentifier },
            ObserverState.Empty with { Identifier = _reactorObserverKey.ObserverId }
        ]);
        _reactorDefinitionsStorage.GetAll().Returns([
            new ReactorDefinition(PatternCapture.ObserverIdentifier, ReactorOwner.Kernel, EventSequenceId.Log, [], false),
            new ReactorDefinition(_reactorObserverKey.ObserverId, ReactorOwner.Client, EventSequenceId.Log, [])
        ]);
    }
}
