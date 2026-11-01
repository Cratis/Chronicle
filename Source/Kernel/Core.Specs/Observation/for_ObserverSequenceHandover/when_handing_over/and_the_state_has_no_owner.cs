// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class and_the_state_has_no_owner : given.a_handover
{
    void Establish() => _current = _current with { Owner = null };

    async Task Because() => _result = await HandOver();

    [Fact] void should_report_the_handover() => _result.ShouldEqual(SequenceHandoverResult.HandedOver);
    [Fact] void should_park_the_progress_as_the_sequence_it_leaves() => _checkpoints.Received(1).Save(Arg.Is<ObserverSequenceCheckpoint>(checkpoint => checkpoint.EventSequenceId == _from));
    [Fact] void should_expect_no_owner() => _observers.Received(1).HandOver(_observerId, Arg.Is<EventSequenceId?>(owner => owner == null), Arg.Any<ObserverSequenceCheckpoint>());
}
