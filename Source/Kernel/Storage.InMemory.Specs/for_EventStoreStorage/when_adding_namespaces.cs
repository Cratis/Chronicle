// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorage;

public class when_adding_namespaces : Specification
{
    const int NamespaceCount = 50;

    EventStoreStorage _store;
    IEventTypesStorage _eventTypesBefore;
    IObserverDefinitionsStorage _observersBefore;
    IEventStoreNamespaceStorage _first;
    IEventStoreNamespaceStorage _firstAgain;

    void Establish()
    {
        _store = new("Studio", _ => Substitute.For<ISinks>(), Substitute.For<Cratis.Choreography.Storage.IJobsStorage>());
        _eventTypesBefore = _store.EventTypes;
        _observersBefore = _store.Observers;
    }

    void Because()
    {
        _first = _store.GetNamespace("ns-0");
        for (var index = 1; index < NamespaceCount; index++)
        {
            _store.GetNamespace($"ns-{index}");
        }

        _firstAgain = _store.GetNamespace("ns-0");
    }

    [Fact] void should_keep_one_event_type_registry_for_the_event_store() => ReferenceEquals(_store.EventTypes, _eventTypesBefore).ShouldBeTrue();
    [Fact] void should_keep_one_observer_definition_registry_for_the_event_store() => ReferenceEquals(_store.Observers, _observersBefore).ShouldBeTrue();
    [Fact] void should_serve_the_same_namespace_storage_again() => ReferenceEquals(_firstAgain, _first).ShouldBeTrue();
}
