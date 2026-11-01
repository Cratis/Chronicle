// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class to_a_sequence_it_has_not_observed : given.a_handover
{
    async Task Because() => _result = await HandOver();

    [Fact] void should_report_the_handover() => _result.ShouldEqual(SequenceHandoverResult.HandedOver);
    [Fact] void should_park_the_stored_progress_of_the_sequence_it_leaves() => _checkpoints.Received(1).Save(Arg.Is<ObserverSequenceCheckpoint>(checkpoint => checkpoint.EventSequenceId == _from && checkpoint.LastHandledEventSequenceNumber.Value == 42UL));
    [Fact] void should_select_a_fresh_start_for_the_target() => _observers.Received(1).HandOver(_observerId, _from, Arg.Is<ObserverSequenceCheckpoint>(checkpoint => checkpoint.EventSequenceId == _to && checkpoint.NextEventSequenceNumber == EventSequenceNumber.First));
}
