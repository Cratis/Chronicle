// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_recovery_deferral_is_stale : given.a_webhook_observer
{
    FailedPartition _failure;
    PartitionDeferral _deferral;

    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 42UL, ["503"], "Stack");
        _failure = _failedPartitionsState.Partitions.Single();
        _deferral = _failure.Deferral!;
        _storageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
    }

    async Task Because() => await _observer.PartitionFailed("partition", 43UL, ["Stale failure"], "Stack", episodeId: _failure.Id, deferralId: Guid.NewGuid());

    [Fact] void should_keep_only_the_current_attempt() => _failure.Attempts.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_current_deferral() => _failure.Deferral.ShouldEqual(_deferral);
    [Fact] void should_not_write_observer_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_failed_partitions() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
}
