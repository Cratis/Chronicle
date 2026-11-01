// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class and_the_state_is_already_on_the_target : given.a_handover
{
    void Establish()
    {
        _current = _current with { Owner = _to };
        _failures = new() { Partitions = [new() { ObserverId = _observerId }] };
    }

    async Task Because() => _result = await HandOver();

    [Fact] void should_report_that_it_is_already_on_the_target() => _result.ShouldEqual(SequenceHandoverResult.AlreadyOnTarget);
    [Fact] void should_not_park_anything() => _checkpoints.DidNotReceive().Save(Arg.Any<ObserverSequenceCheckpoint>());
    [Fact] void should_not_select_anything() => _observers.DidNotReceive().HandOver(Arg.Any<ObserverId>(), Arg.Any<EventSequenceId?>(), Arg.Any<ObserverSequenceCheckpoint>());
    [Fact] void should_not_write_failed_partitions() => _failedPartitions.DidNotReceive().Save(Arg.Any<ObserverId>(), Arg.Any<FailedPartitions>());
}
