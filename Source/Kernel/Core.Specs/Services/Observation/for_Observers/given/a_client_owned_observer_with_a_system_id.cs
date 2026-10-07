// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.given;

public class a_client_owned_observer_with_a_system_id : application_and_system_observers
{
    void Establish() => _observerDefinitionsStorage.GetAll().Returns(
    [
        new ObserverDefinition(SystemObserverId, [new EventType("a-recorded", 1)], Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true)
    ]);
}
