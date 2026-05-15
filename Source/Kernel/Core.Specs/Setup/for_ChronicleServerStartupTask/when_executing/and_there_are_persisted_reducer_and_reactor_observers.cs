// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_there_are_persisted_reducer_and_reactor_observers : given.a_startup_task
{
    ObserverKey _unknownReducerObserverKey = ObserverKey.NotSet;
    ObserverKey _unknownReactorObserverKey = ObserverKey.NotSet;

    void Establish()
    {
        _unknownReducerObserverKey = new ObserverKey("unknown-reducer", _eventStore, _namespace, EventSequenceId.Log);
        _unknownReactorObserverKey = new ObserverKey("unknown-reactor", _eventStore, _namespace, EventSequenceId.Log);

        _observerStateStorage.GetAll().Returns(Task.FromResult<IEnumerable<ObserverState>>([
            ObserverState.Empty with { Identifier = _reducerObserverKey.ObserverId, RunningState = ObserverRunningState.Active },
            ObserverState.Empty with { Identifier = _reactorObserverKey.ObserverId, RunningState = ObserverRunningState.Active }
        ]));

        _reducerDefinitionsStorage.GetAll().Returns(Task.FromResult<IEnumerable<ReducerDefinition>>([
            new ReducerDefinition(_reducerObserverKey.ObserverId, EventSequenceId.Log, [], "read-model", true, SinkDefinition.None),
            new ReducerDefinition(_unknownReducerObserverKey.ObserverId, EventSequenceId.Log, [], "read-model", true, SinkDefinition.None)
        ]));

        _reactorDefinitionsStorage.GetAll().Returns(Task.FromResult<IEnumerable<ReactorDefinition>>([
            new ReactorDefinition(_reactorObserverKey.ObserverId, ReactorOwner.Client, EventSequenceId.Log, []),
            new ReactorDefinition(_unknownReactorObserverKey.ObserverId, ReactorOwner.Client, EventSequenceId.Log, [])
        ]));
    }

    Task Because() => Execute();

    [Fact] void should_rehydrate_the_persisted_reducer_observer() => _reducerObserver.Received(1).Ensure();
    [Fact] void should_rehydrate_the_persisted_reactor_observer() => _reactorObserver.Received(1).Ensure();
    [Fact] void should_not_rehydrate_reducer_observers_without_persisted_state() => _grainFactory.DidNotReceive().GetGrain<Cratis.Chronicle.Observation.IObserver>(_unknownReducerObserverKey);
    [Fact] void should_not_rehydrate_reactor_observers_without_persisted_state() => _grainFactory.DidNotReceive().GetGrain<Cratis.Chronicle.Observation.IObserver>(_unknownReactorObserverKey);
}
