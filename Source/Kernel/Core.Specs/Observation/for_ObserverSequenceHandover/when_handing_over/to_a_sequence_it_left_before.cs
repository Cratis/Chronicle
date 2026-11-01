// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class to_a_sequence_it_left_before : given.a_handover
{
    void Establish() => _checkpoints.Get(_observerId, _to).Returns(ObserverSequenceCheckpoint.Fresh(_observerId, _to) with { NextEventSequenceNumber = 100UL });

    async Task Because() => _result = await HandOver();

    [Fact] void should_report_the_handover() => _result.ShouldEqual(SequenceHandoverResult.HandedOver);
    [Fact] void should_select_the_parked_progress_of_the_target() => _observers.Received(1).HandOver(_observerId, _from, Arg.Is<ObserverSequenceCheckpoint>(checkpoint => checkpoint.EventSequenceId == _to && checkpoint.NextEventSequenceNumber.Value == 100UL));
}
