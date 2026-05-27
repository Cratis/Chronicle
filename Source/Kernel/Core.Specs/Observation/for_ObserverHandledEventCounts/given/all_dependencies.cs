// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_ObserverHandledEventCounts.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IObserverDefinitionsStorage _observerDefinitionsStorage;
    protected IObserverStateStorage _observerStateStorage;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected ObserverHandledEventCounts _grain;
    protected ObserversKey _observersKey;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _observerDefinitionsStorage = Substitute.For<IObserverDefinitionsStorage>();
        _observerStateStorage = Substitute.For<IObserverStateStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();

        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.Observers.Returns(_observerDefinitionsStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.Observers.Returns(_observerStateStorage);
        _namespaceStorage.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(_eventSequenceStorage);

        _grain = new ObserverHandledEventCounts(_storage, Substitute.For<ILogger<ObserverHandledEventCounts>>());

        _observersKey = new ObserversKey("some-event-store", "some-namespace");
        typeof(ObserverHandledEventCounts)
            .GetField("_observersKey", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_grain, _observersKey);
    }
}
