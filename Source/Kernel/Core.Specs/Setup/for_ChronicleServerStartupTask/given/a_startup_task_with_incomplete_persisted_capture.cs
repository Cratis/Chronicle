// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

public class a_startup_task_with_incomplete_persisted_capture : a_startup_task_with_persisted_capture
{
    void Establish()
    {
        // Subscription persisted state and the shared definition before the reactor definition could be saved.
        _reactorDefinitionsStorage.GetAll().Returns([
            new ReactorDefinition(_reactorObserverKey.ObserverId, ReactorOwner.Client, EventSequenceId.Log, [])
        ]);
        _eventStoreStorage.Observers.GetAll().Returns([
            new ObserverDefinition { Identifier = PatternCapture.ObserverIdentifier, EventSequenceId = EventSequenceId.Log }
        ]);
    }
}
