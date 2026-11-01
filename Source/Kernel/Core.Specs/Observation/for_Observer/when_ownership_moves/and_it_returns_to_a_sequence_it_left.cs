// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_it_returns_to_a_sequence_it_left : given.an_observer_with_progress_on_another_sequence
{
    ObserverState _stored;
    ObserverState _active;

    void Establish()
    {
        MoveStoredDefinitionTo(_otherSequence);
        var parked = ObserverSequenceCheckpoint.Fresh(_observerId, _observerKey.EventSequenceId) with
        {
            NextEventSequenceNumber = 200UL,
            LastHandledEventSequenceNumber = 199UL
        };
        _checkpoints.Get(_observerId, _observerKey.EventSequenceId).Returns(parked);
    }

    async Task Because()
    {
        await Subscribe();
        _stored = await _guarded.Get(_observerId);
        _active = await _observer.GetState();
    }

    [Fact] void should_own_the_state_on_the_sequence_it_returns_to() => _stored.Owner.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_resume_the_parked_watermark() => _stored.LastHandledEventSequenceNumber.Value.ShouldEqual(199UL);
    [Fact] void should_resume_the_parked_position() => _stored.NextEventSequenceNumber.Value.ShouldEqual(200UL);
    [Fact] void should_load_the_parked_watermark_into_the_activation() => _active.LastHandledEventSequenceNumber.Value.ShouldEqual(199UL);
}
