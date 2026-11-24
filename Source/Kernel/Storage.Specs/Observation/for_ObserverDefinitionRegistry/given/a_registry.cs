// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.given;

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry.given;

public class a_registry : Specification
{
    protected static readonly ObserverId _observerId = "an-observer";
    protected static readonly ObserverId _otherObserverId = "another-observer";
    protected static readonly EventType _firstEventType = new("first-event", 1);
    protected static readonly EventType _secondEventType = new("second-event", 1);

    protected PlainObserverDefinitionsStorage _storage;
    protected ManualTimeProvider _time;
    protected ObserverDefinitionRegistry _registry;

    void Establish()
    {
        _storage = new();
        _storage.Seed(DefinitionOf(_observerId, _firstEventType));
        _storage.Seed(DefinitionOf(_otherObserverId, _firstEventType));
        _time = new();
        _registry = new(_storage, _time);
    }

    protected static ObserverDefinition DefinitionOf(ObserverId id, params EventType[] eventTypes) =>
        new(id, eventTypes, EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true);
}
