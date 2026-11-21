// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_an_observer_with_a_definition_rejects_its_address : given.a_namespace_remover
{
    ObserverKeyDoesNotMatchDefinition _failure;
    Exception? _error;

    void Establish()
    {
        var key = new ObserverKey(_observerId, _eventStore, _namespace, EventSequenceId.Log);
        _failure = new ObserverKeyDoesNotMatchDefinition(key, "inbox-StudioAdmin");
        _observer.Unsubscribe().Returns(Task.FromException(_failure));
    }

    async Task Because() => _error = await Catch.Exception(() => _remover.Remove(_eventStore, _namespace));

    [Fact] void should_propagate_the_mismatch() => _error.ShouldEqual(_failure);
    [Fact] async Task should_not_remove_the_observer() => await _observer.DidNotReceive().Remove();
    [Fact] async Task should_not_drop_the_namespace() => await _eventStoreStorage.DidNotReceive().DropNamespace(_namespace);
}
