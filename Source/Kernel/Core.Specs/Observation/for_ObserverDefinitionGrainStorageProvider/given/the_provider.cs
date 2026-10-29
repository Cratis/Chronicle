// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider.given;

public class the_provider : Specification
{
    protected static readonly ObserverId _observerId = "an-observer";
    protected static readonly EventSequenceId _otherEventSequenceId = "inbox-another-store";
    protected ObserverDefinitionGrainStorageProvider _provider;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IObserverDefinitionsStorage _definitionsStorage;
    protected ObserverKey _observerKey;
    protected GrainId _grainId;
    protected GrainState<ObserverDefinition> _grainState;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _definitionsStorage = Substitute.For<IObserverDefinitionsStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.Observers.Returns(_definitionsStorage);
        _definitionsStorage.Get(Arg.Any<ObserverId>()).Returns(ObserverDefinition.Empty);
        _provider = new(_storage);

        _observerKey = new(_observerId, "an-event-store", "a-namespace", EventSequenceId.Log);
        _grainId = GrainId.Create("type", _observerKey.ToString());
        _grainState = new() { State = new() };
    }

    protected ObserverDefinition DefinitionOn(EventSequenceId eventSequenceId) =>
        new(_observerId, [], eventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true);
}
