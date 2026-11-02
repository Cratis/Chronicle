// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// The automatic recovery loop skips individually quarantined partitions (<c language="csharp">.Where(p => !p.IsQuarantined)</c> in
/// <see cref="Observer.TryRecoverAllFailedPartitions"/>), but a manual retry request used not to check this at all -
/// it would happily kick off a job for a partition the automatic loop was deliberately leaving alone, and report
/// success while doing something the system had decided not to do on its own.
/// </summary>
public class when_recovering_a_failed_partition_that_is_individually_quarantined : given.an_observer_with_subscription
{
    readonly given.RecordingObserverLogger _recordedLogger = new();
    const string Partition = "SomePartition";
    PartitionRecoveryOutcome _outcome;

    protected override ILogger<Observer> CreateLogger() => _recordedLogger;

    void Establish() => _failedPartitionsState.Partitions =
    [
        new()
        {
            Partition = (Key)Partition,
            IsQuarantined = true
        }
    ];

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition((Key)Partition);

    [Fact] void should_report_partition_quarantined() => _outcome.ShouldEqual(PartitionRecoveryOutcome.PartitionQuarantined);

    [Fact] void should_warn_that_the_retry_did_not_start() => _recordedLogger.Warnings.ShouldContain(_ =>
        _.Contains(Partition, StringComparison.Ordinal) &&
        _.Contains(_observerId.Value, StringComparison.OrdinalIgnoreCase) &&
        _.Contains(nameof(PartitionRecoveryOutcome.PartitionQuarantined), StringComparison.Ordinal));

    [Fact] void should_not_start_a_recover_job() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
