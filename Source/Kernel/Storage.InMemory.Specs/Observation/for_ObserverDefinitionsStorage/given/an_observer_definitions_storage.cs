// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage.given;

public class an_observer_definitions_storage : Specification
{
    protected static readonly ObserverId _observerId = "an-observer";
    protected static readonly EventType _firstEventType = new("first-event", 1);
    protected static readonly EventType _secondEventType = new("second-event", 1);
    protected ObserverDefinitionsStorage _storage;

    void Establish() => _storage = new();

    protected static ObserverDefinition DefinitionObserving(params EventType[] eventTypes) =>
        new(_observerId, eventTypes, EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true);
}
