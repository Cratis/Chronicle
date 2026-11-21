// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverTransitionStateMerge.when_merging;

public class and_the_failed_partition_count_has_been_reconciled : given.a_transition_hook
{
    void Establish()
    {
        _handed = _handed with { FailedPartitionCount = 1 };
        _recorded = _handed;

        // The observer reconciles the returned count with live failures before merging recorded progress.
        _returned = _handed with { FailedPartitionCount = 0 };
    }

    void Because() => _result = ObserverTransitionStateMerge.Merge(_handed, _recorded, _returned);

    [Fact] void should_not_restore_the_recorded_snapshot_count() => _result.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
}
