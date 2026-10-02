// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_partition_quarantine;

/// <summary>
/// Clearing starts a new retry budget: with a budget of two, the next two failures are retried rather than
/// quarantined, and only the one after that quarantines the partition again.
/// </summary>
public class and_the_partition_fails_again : given.a_quarantined_partition
{
    bool _quarantinedAfterFirstFailure;
    bool _quarantinedAfterSecondFailure;
    bool _quarantinedAfterThirdFailure;

    async Task Establish() => await _observer.ClearPartitionQuarantine((Key)Partition, false);

    async Task Because()
    {
        await _observer.PartitionFailed((Key)Partition, 45UL, ["again 1"], string.Empty);
        _quarantinedAfterFirstFailure = FailedPartition.IsQuarantined;
        await _observer.PartitionFailed((Key)Partition, 45UL, ["again 2"], string.Empty);
        _quarantinedAfterSecondFailure = FailedPartition.IsQuarantined;
        await _observer.PartitionFailed((Key)Partition, 45UL, ["again 3"], string.Empty);
        _quarantinedAfterThirdFailure = FailedPartition.IsQuarantined;
    }

    [Fact] void should_not_quarantine_after_the_first_failure() => _quarantinedAfterFirstFailure.ShouldBeFalse();
    [Fact] void should_not_quarantine_after_the_second_failure() => _quarantinedAfterSecondFailure.ShouldBeFalse();
    [Fact] void should_quarantine_once_the_new_budget_is_exhausted() => _quarantinedAfterThirdFailure.ShouldBeTrue();
    [Fact] void should_keep_the_whole_history() => FailedPartition.Attempts.Count().ShouldEqual(6);
}
