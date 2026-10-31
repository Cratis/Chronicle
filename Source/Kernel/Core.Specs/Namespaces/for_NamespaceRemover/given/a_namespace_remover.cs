// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover.given;

public class a_namespace_remover : Specification
{
    protected static readonly EventStoreName _eventStore = "some-store";
    protected static readonly EventStoreNamespaceName _namespace = "tenant";
    protected static readonly ObserverId _observerId = "some-observer";

    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected INamespaceStorage _namespaceRecords;
    protected IObserverDefinitionsStorage _definitions;
    protected IObserverStateStorage _observerStates;
    protected INamespaces _namespaces;
    protected IReminderTable _reminderTable;
    protected IObserver _observerWithoutState;
    protected static readonly ObserverKey _keyWithoutState = new("Some->Other", _eventStore, _namespace, EventSequenceId.Outbox);
    protected IObserver _observer;
    protected NamespaceRemover _remover;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _namespaceRecords = Substitute.For<INamespaceStorage>();
        _definitions = Substitute.For<IObserverDefinitionsStorage>();
        _observerStates = Substitute.For<IObserverStateStorage>();
        _namespaces = Substitute.For<INamespaces>();
        _observer = Substitute.For<IObserver>();
        _observerWithoutState = Substitute.For<IObserver>();
        _reminderTable = Substitute.For<IReminderTable>();
        _reminderTable.ReadRows(Arg.Any<uint>(), Arg.Any<uint>()).Returns(callInfo => Task.FromResult(callInfo.ArgAt<uint>(0) == 0
            ? new ReminderTableData([
                Reminder(_keyWithoutState.ToString()),
                Reminder(new ObserverKey("Other", _eventStore, "another-namespace", EventSequenceId.Log).ToString())])
            : new ReminderTableData([])));

        _storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(_namespace).Returns(_namespaceStorage);
        _eventStoreStorage.Namespaces.Returns(_namespaceRecords);
        _eventStoreStorage.Observers.Returns(_definitions);
        _namespaceStorage.Observers.Returns(_observerStates);
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([EventSequenceId.Log]));

        _definitions.GetAll().Returns(Task.FromResult<IEnumerable<ObserverDefinition>>(
            [new ObserverDefinition(_observerId, [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true)]));
        _observerStates.GetAll().Returns(Task.FromResult<IEnumerable<ObserverState>>([ObserverState.Empty with { Identifier = _observerId }]));

        _grainFactory.GetGrain<INamespaces>(_eventStore).Returns(_namespaces);
        _grainFactory.GetGrain<IObserver>(new ObserverKey(_observerId, _eventStore, _namespace, EventSequenceId.Log)).Returns(_observer);

        _grainFactory.GetGrain<IObserver>(_keyWithoutState).Returns(_observerWithoutState);

        _remover = new NamespaceRemover(_grainFactory, _storage, _reminderTable, NullLogger<NamespaceRemover>.Instance);
    }

    static ReminderEntry Reminder(string key) => new()
    {
        GrainId = GrainId.Create("observer", key),
        ReminderName = "chronicle-observer:alerts",
        ETag = "etag"
    };
}
