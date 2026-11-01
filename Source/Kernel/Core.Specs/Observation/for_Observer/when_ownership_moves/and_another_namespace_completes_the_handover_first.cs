// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_another_namespace_completes_the_handover_first : given.an_observer_with_progress_on_another_sequence
{
    ObserverState _stored;
    ObserverState _active;

    void Establish()
    {
        MoveStoredDefinitionTo(_otherSequence);

        // A registration through another namespace hands this namespace over after this activation loaded its progress.
        _eventStoreStorage.Namespaces.GetAll().Returns(async _ =>
        {
            await _guarded.HandOver(_observerId, _otherSequence, ObserverSequenceCheckpoint.Fresh(_observerId, _observerKey.EventSequenceId));
            return (IEnumerable<NamespaceState>)[new(_observerKey.Namespace, DateTimeOffset.UnixEpoch)];
        });
    }

    async Task Because()
    {
        await Subscribe();
        _stored = await _guarded.Get(_observerId);
        _active = await _observer.GetState();
    }

    [Fact] void should_own_the_state_on_the_new_sequence() => _stored.Owner.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_load_the_new_progress_into_the_activation() => _active.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_write_the_cursor_of_the_sequence_it_left_over_the_new_progress() => _stored.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
