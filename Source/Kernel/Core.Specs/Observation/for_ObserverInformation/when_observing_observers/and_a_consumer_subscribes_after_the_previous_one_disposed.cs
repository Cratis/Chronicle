// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverInformation.when_observing_observers;

public class and_a_consumer_subscribes_after_the_previous_one_disposed : Specification
{
    readonly Storage.InMemory.Observation.ObserverDefinitionsStorage _definitions = new();
    readonly Storage.InMemory.Observation.ObserverStateStorage _states = new();
    int _definitionSubscriptions;
    int _stateSubscriptions;
    ISubject<IEnumerable<ObserverInformation>> _result;
    IDisposable _first;
    IDisposable _second;
    IEnumerable<ObserverInformation> _latest;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        var definitionsStorage = Substitute.For<IObserverDefinitionsStorage>();
        var stateStorage = Substitute.For<IObserverStateStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.Observers.Returns(definitionsStorage);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(@namespace);
        @namespace.Observers.Returns(stateStorage);
        definitionsStorage.ObserveAll().Returns(Subject.Create<IEnumerable<ObserverDefinition>>(
            System.Reactive.Observer.Create<IEnumerable<ObserverDefinition>>(_ => { }),
            Observable.Defer(() =>
            {
                _definitionSubscriptions++;
                return _definitions.ObserveAll();
            })));
        stateStorage.ObserveAll().Returns(Subject.Create<IEnumerable<ObserverState>>(
            System.Reactive.Observer.Create<IEnumerable<ObserverState>>(_ => { }),
            Observable.Defer(() =>
            {
                _stateSubscriptions++;
                return _states.ObserveAll();
            })));
        _result = ObserverInformation.ObserveObservers("store", "namespace", storage);
        _first = _result.Subscribe(_ => { });
        _first.Dispose();
        _second = _result.Subscribe(observers => _latest = observers);
    }

    void Because() => _definitions.Save(new("changed", [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true)).GetAwaiter().GetResult();

    [Fact] void should_open_a_definitions_subscription_per_consumer() => _definitionSubscriptions.ShouldEqual(2);
    [Fact] void should_open_a_states_subscription_per_consumer() => _stateSubscriptions.ShouldEqual(2);
    [Fact] void should_deliver_the_change_to_the_new_consumer() => _latest.Any(_ => _.Id == "changed").ShouldBeTrue();

    void Destroy()
    {
        _second.Dispose();
        _states.Dispose();
        _definitions.Dispose();
    }
}
