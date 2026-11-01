// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class with_failed_partitions_tagged_for_the_target : given.a_handover
{
    void Establish() => _failures = new()
    {
        Partitions =
        [
            new() { ObserverId = _observerId, EventSequenceId = _to },
            new() { ObserverId = _observerId, EventSequenceId = _from }
        ]
    };

    async Task Because() => _result = await HandOver();

    [Fact] void should_count_only_the_failures_of_the_target() => _observers.Received(1).HandOver(_observerId, _from, Arg.Is<ObserverSequenceCheckpoint>(checkpoint => checkpoint.FailedPartitionCount.Value == 1));
    [Fact] void should_not_rewrite_tagged_failures() => _failedPartitions.DidNotReceive().Save(Arg.Any<ObserverId>(), Arg.Any<FailedPartitions>());
}
