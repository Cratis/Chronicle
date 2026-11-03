// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

/// <summary>
/// A startup task whose prerequisites all succeed and whose namespace has exactly one persisted application reactor to activate in the background.
/// </summary>
public class a_startup_task_with_a_persisted_reactor : a_startup_task
{
    void Establish()
    {
        _observerStateStorage.GetAll().Returns(Task.FromResult<IEnumerable<ObserverState>>([
            ObserverState.Empty with { Identifier = _reactorObserverKey.ObserverId, RunningState = ObserverRunningState.Active }
        ]));
        _reactorDefinitionsStorage.GetAll().Returns(Task.FromResult<IEnumerable<ReactorDefinition>>([
            new ReactorDefinition(_reactorObserverKey.ObserverId, ReactorOwner.Client, EventSequenceId.Log, [])
        ]));
    }
}
