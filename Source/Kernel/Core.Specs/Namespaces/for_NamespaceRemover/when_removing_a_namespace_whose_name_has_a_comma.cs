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

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_removing_a_namespace_whose_name_has_a_comma : Specification
{
    static readonly EventStoreName _eventStore = "some-store";
    static readonly EventStoreNamespaceName _namespace = "tenant,tenant";
    static readonly ObserverId _observerId = "some-observer";
    static readonly ObserverKey _resubscribedKey = new("Some->Other", _eventStore, _namespace, EventSequenceId.Outbox);

    IGrainFactory _grainFactory;
    IStorage _storage;
    IEventStoreStorage _eventStoreStorage;
    INamespaceStorage _namespaceRecords;
    INamespaces _namespaces;
    IReminderTable _reminderTable;
    IObserver _observer;
    ReminderEntry _resubscribed;
    bool _listed;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _namespaceRecords = Substitute.For<INamespaceStorage>();
        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        var observerStates = Substitute.For<IObserverStateStorage>();
        _namespaces = Substitute.For<INamespaces>();
        _observer = Substitute.For<IObserver>();
        _reminderTable = Substitute.For<IReminderTable>();
        _resubscribed = new() { GrainId = GrainId.Create("observer", _resubscribedKey.ToString()), ReminderName = "chronicle-observer:alerts", ETag = "etag" };

        // While the namespace is listed its observers activate; afterwards the only thing left of one is its reminder.
        _listed = true;
        _namespaces.When(_ => _.Forget(_namespace)).Do(_ => _listed = false);
        _observer.Remove().Returns(_ => _listed ? Task.CompletedTask : throw new InvalidNamespaceName(_eventStore, _namespace));
        _reminderTable.ReadRows(Arg.Any<uint>(), Arg.Any<uint>()).Returns(callInfo => Task.FromResult(callInfo.ArgAt<uint>(0) == 0 && !_listed
            ? new ReminderTableData([_resubscribed])
            : new ReminderTableData([])));

        _storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(_namespace).Returns(namespaceStorage);
        _eventStoreStorage.Namespaces.Returns(_namespaceRecords);
        _eventStoreStorage.Observers.Returns(definitions);
        namespaceStorage.Observers.Returns(observerStates);
        namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([EventSequenceId.Log]));
        definitions.GetAll().Returns(Task.FromResult<IEnumerable<ObserverDefinition>>(
            [new ObserverDefinition(_observerId, [], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true)]));
        observerStates.GetAll().Returns(Task.FromResult<IEnumerable<ObserverState>>([ObserverState.Empty with { Identifier = _observerId }]));

        _grainFactory.GetGrain<INamespaces>(_eventStore).Returns(_namespaces);
        _grainFactory.GetGrain<IObserver>(new ObserverKey(_observerId, _eventStore, _namespace, EventSequenceId.Log)).Returns(_observer);
    }

    async Task Because() => await new NamespaceRemover(_grainFactory, _storage, _reminderTable, NullLogger<NamespaceRemover>.Instance, NamespaceCasingContext.Create())
        .Remove(_eventStore, _namespace);

    [Fact] async Task should_remove_the_observer_while_the_namespace_is_still_listed() => await _observer.Received(1).Remove();
    [Fact] async Task should_forget_the_namespace() => await _namespaces.Received(1).Forget(_namespace);
    [Fact] async Task should_remove_the_reminder_of_an_observer_subscribed_again_directly() => await _reminderTable.Received(1).RemoveRow(_resubscribed.GrainId, _resubscribed.ReminderName, _resubscribed.ETag);
    [Fact] async Task should_drop_the_data_of_the_namespace() => await _eventStoreStorage.Received(1).DropNamespace(_namespace);
    [Fact] async Task should_delete_the_namespace_record() => await _namespaceRecords.Received(1).Delete(_namespace);
}
