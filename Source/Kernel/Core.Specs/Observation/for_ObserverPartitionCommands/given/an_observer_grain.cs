// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverPartitionCommands.given;

public class an_observer_grain : Specification
{
    protected const string EventStore = "some-event-store";
    protected const string Namespace = "some-namespace";
    protected const string ObserverIdentifier = "some-observer";
    protected const string Partition = "some-partition";

    protected readonly ObserverKey _key = new(ObserverIdentifier, EventStore, Namespace, "inbox-source");
    protected IStorage _storage;
    protected IGrainFactory _grainFactory;
    protected IObserver _observer;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        _storage.GetEventStore(_key.EventStore).Returns(eventStoreStorage);
        eventStoreStorage.Observers.Returns(definitions);
        definitions.Get(_key.ObserverId).Returns(new ObserverDefinition(
            _key.ObserverId, [], _key.EventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true));
        _observer = Substitute.For<IObserver>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }
}
