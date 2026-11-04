// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverInformation.when_observing_observers;

public class and_the_last_consumer_disposes : Specification
{
    readonly Storage.InMemory.Observation.ObserverDefinitionsStorage _definitions = new();
    readonly Storage.InMemory.Observation.ObserverStateStorage _states = new();
    int _definitionSubscriptions;
    int _stateSubscriptions;
    int _definitionSubscriptionsBeforeObserving;
    int _stateSubscriptionsBeforeObserving;
    int _definitionDisposals;
    int _stateDisposals;
    ISubject<IEnumerable<ObserverInformation>> _result;
    IDisposable _first;

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
                return _definitions.ObserveAll().Finally(() => _definitionDisposals++);
            })));
        stateStorage.ObserveAll().Returns(Subject.Create<IEnumerable<ObserverState>>(
            System.Reactive.Observer.Create<IEnumerable<ObserverState>>(_ => { }),
            Observable.Defer(() =>
            {
                _stateSubscriptions++;
                return _states.ObserveAll().Finally(() => _stateDisposals++);
            })));
        _result = ObserverInformation.ObserveObservers("store", "namespace", storage);
        _definitionSubscriptionsBeforeObserving = _definitionSubscriptions;
        _stateSubscriptionsBeforeObserving = _stateSubscriptions;
        _first = _result.Subscribe(_ => { });
    }

    void Because()
    {
        _first.Dispose();
    }

    [Fact] void should_not_subscribe_to_definitions_before_observing() => _definitionSubscriptionsBeforeObserving.ShouldEqual(0);
    [Fact] void should_not_subscribe_to_states_before_observing() => _stateSubscriptionsBeforeObserving.ShouldEqual(0);
    [Fact] void should_subscribe_once_to_definitions() => _definitionSubscriptions.ShouldEqual(1);
    [Fact] void should_subscribe_once_to_states() => _stateSubscriptions.ShouldEqual(1);
    [Fact] void should_dispose_definitions_when_the_consumer_disposes() => _definitionDisposals.ShouldEqual(1);
    [Fact] void should_dispose_states_when_the_consumer_disposes() => _stateDisposals.ShouldEqual(1);

    void Destroy()
    {
        _first.Dispose();
        _states.Dispose();
        _definitions.Dispose();
    }
}
