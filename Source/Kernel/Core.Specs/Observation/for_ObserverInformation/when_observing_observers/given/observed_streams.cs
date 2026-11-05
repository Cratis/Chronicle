// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverInformation.when_observing_observers.given;

public class observed_streams : Specification
{
    protected BehaviorSubject<IEnumerable<ObserverDefinition>> _definitions = new([Definition("first")]);
    protected Subject<IEnumerable<ObserverState>> _states = new();
    protected IObserverDefinitionsStorage _definitionsStorage;
    protected List<ObserverInformation[]> _emissions = [];
    protected ISubject<IEnumerable<ObserverInformation>> _result;
    IDisposable _subscription;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        _definitionsStorage = Substitute.For<IObserverDefinitionsStorage>();
        var stateStorage = Substitute.For<IObserverStateStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.Observers.Returns(_definitionsStorage);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(@namespace);
        @namespace.Observers.Returns(stateStorage);
        _definitionsStorage.ObserveAll().Returns(_definitions);
        stateStorage.ObserveAll().Returns(_states);
        _result = ObserverInformation.ObserveObservers("store", "namespace", storage);
        _subscription = _result.Subscribe(observers => _emissions.Add(observers.ToArray()));
    }

    protected static ObserverDefinition Definition(string id) => new(id, [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true);

    void Destroy()
    {
        _subscription.Dispose();
        _states.Dispose();
        _definitions.Dispose();
    }
}
