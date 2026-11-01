// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_the_new_sequence_is_new_to_it : given.an_observer_with_progress_on_another_sequence
{
    ObserverState _stored;
    ObserverState _active;

    void Establish() => MoveStoredDefinitionTo(_otherSequence);

    async Task Because()
    {
        await Subscribe();
        _stored = await _guarded.Get(_observerId);
        _active = await _observer.GetState();
    }

    [Fact] void should_own_the_state_on_the_new_sequence() => _stored.Owner.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_start_from_the_first_event() => _stored.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_inherit_the_cursor_of_the_sequence_it_left() => _stored.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_park_the_sequence_it_left() => _checkpoints.Received(1).Save(Arg.Is<ObserverSequenceCheckpoint>(checkpoint => checkpoint.EventSequenceId == _otherSequence && checkpoint.LastHandledEventSequenceNumber.Value == 42UL));
    [Fact] void should_load_the_new_progress_into_the_activation() => _active.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
