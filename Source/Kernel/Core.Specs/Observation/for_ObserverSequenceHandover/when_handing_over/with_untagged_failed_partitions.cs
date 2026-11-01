// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverSequenceHandover.when_handing_over;

public class with_untagged_failed_partitions : given.a_handover
{
    FailedPartition _untagged;
    FailedPartition _tagged;

    void Establish()
    {
        _untagged = new() { ObserverId = _observerId };
        _tagged = new() { ObserverId = _observerId, EventSequenceId = _to };
        _failures = new() { Partitions = [_untagged, _tagged] };
    }

    async Task Because() => _result = await HandOver();

    [Fact] void should_stamp_the_untagged_failure_with_the_sequence_it_leaves() => _untagged.EventSequenceId.ShouldEqual(_from);
    [Fact] void should_write_only_the_untagged_failure() => _failedPartitions.Received(1).Save(_observerId, Arg.Is<FailedPartitions>(failures => failures.Partitions.Count() == 1 && failures.Partitions.Single().Id == _untagged.Id));
    [Fact] void should_leave_the_tagged_failure_alone() => _tagged.EventSequenceId.ShouldEqual(_to);
}
