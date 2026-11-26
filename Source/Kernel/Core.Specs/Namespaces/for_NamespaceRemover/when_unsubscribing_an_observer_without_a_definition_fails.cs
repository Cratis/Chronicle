// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_unsubscribing_an_observer_without_a_definition_fails : given.a_namespace_remover
{
    ObserverRemovalNotAllowed _failure;
    Exception? _error;

    void Establish()
    {
        _definitions.GetAll().Returns(Task.FromResult<IEnumerable<ObserverDefinition>>([]));
        _failure = new ObserverRemovalNotAllowed(new ObserverKey(_observerId, _eventStore, _namespace, EventSequenceId.Log));
        _observer.Unsubscribe().Returns(Task.FromException(_failure));
    }

    async Task Because() => _error = await Catch.Exception(() => _remover.Remove(_eventStore, _namespace));

    [Fact] void should_propagate_the_error() => _error.ShouldEqual(_failure);
    [Fact] async Task should_not_remove_the_observer() => await _observer.DidNotReceive().Remove();
    [Fact] async Task should_not_drop_the_namespace() => await _eventStoreStorage.DidNotReceive().DropNamespace(_namespace);
    [Fact] async Task should_not_delete_the_namespace_record() => await _namespaceRecords.DidNotReceive().Delete(_namespace);
}
