// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_removing_a_namespace_whose_observer_has_no_definition : given.a_namespace_remover
{
    static readonly EventSequenceId _inbox = "inbox-StudioAdmin";
    IObserver _inboxObserver;
    Exception? _error;

    void Establish()
    {
        _definitions.GetAll().Returns(Task.FromResult<IEnumerable<ObserverDefinition>>([]));
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([EventSequenceId.Log, _inbox]));
        var logKey = new ObserverKey(_observerId, _eventStore, _namespace, EventSequenceId.Log);
        _observer.Unsubscribe().Returns(Task.FromException(new ObserverKeyDoesNotMatchDefinition(logKey, _inbox)));
        _inboxObserver = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(new ObserverKey(_observerId, _eventStore, _namespace, _inbox)).Returns(_inboxObserver);
    }

    async Task Because() => _error = await Catch.Exception(() => _remover.Remove(_eventStore, _namespace));

    [Fact] void should_complete_without_an_error() => _error.ShouldBeNull();
    [Fact] async Task should_try_unsubscribing_the_mismatching_address() => await _observer.Received(1).Unsubscribe();
    [Fact] async Task should_not_remove_the_mismatching_address() => await _observer.DidNotReceive().Remove();
    [Fact] async Task should_unsubscribe_the_observer_on_its_own_sequence() => await _inboxObserver.Received(1).Unsubscribe();
    [Fact] async Task should_remove_the_observer_on_its_own_sequence() => await _inboxObserver.Received(1).Remove();
    [Fact] async Task should_drop_the_data_of_the_namespace() => await _eventStoreStorage.Received(1).DropNamespace(_namespace);
    [Fact] async Task should_delete_the_namespace_record() => await _namespaceRecords.Received(1).Delete(_namespace);
    [Fact] async Task should_forget_the_namespace() => await _namespaces.Received(1).Forget(_namespace);
    [Fact] void should_invalidate_the_cached_casing() => _namespaceCasing.Received(1).Forget(_eventStore, _namespace);
}
