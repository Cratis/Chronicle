// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_a_previous_handover_was_interrupted : given.an_observer_with_progress_on_another_sequence
{
    ObserverState _stored;

    async Task Because()
    {
        await Subscribe();
        await Subscribe();
        _stored = await _guarded.Get(_observerId);
    }

    [Fact] void should_complete_the_handover() => _stored.Owner.ShouldEqual(_observerKey.EventSequenceId);
    [Fact] void should_start_the_sequence_from_the_first_event() => _stored.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_park_the_progress_once() => _checkpoints.Received(1).Save(Arg.Is<ObserverSequenceCheckpoint>(checkpoint => checkpoint.EventSequenceId == _otherSequence));
}
