// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemover;

public class when_an_observer_is_subscribed_again_before_it_is_removed : given.a_namespace_remover
{
    int _removals;

    void Establish() =>
        _observer.Remove().Returns(_ => ++_removals == 1 ? Task.FromException(new ObserverRemovalNotAllowed(new ObserverKey(_observerId, _eventStore, _namespace, Concepts.EventSequences.EventSequenceId.Log))) : Task.CompletedTask);

    async Task Because() => await _remover.Remove(_eventStore, _namespace);

    [Fact] void should_try_again() => _removals.ShouldEqual(2);
    [Fact] async Task should_unsubscribe_before_each_attempt() => await _observer.Received(2).Unsubscribe();
    [Fact] async Task should_stop_listing_the_namespace_first() => await _namespaces.Received(1).Forget(_namespace);
}
