// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_completed;

/// <summary>
/// A retry stopped after handling event 3. On resume every remaining event is excluded by the observer's filters, so
/// the step completes with the same watermark. The recovery is reported as complete, not partial, so the failure is cleared.
/// </summary>
public class and_a_stopped_retry_resumed_over_only_excluded_events : given.a_retry_failed_partition_job
{
    void Establish()
    {
        var options = new System.Text.Json.JsonSerializerOptions();
        _stateStorage.State.HandleResult(
            JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(new HandleEventsForPartitionResult(3UL), ["stopped"], "trace")),
            options);
        _stateStorage.State.HandleResult(
            JobStepResult.Succeeded(new HandleEventsForPartitionResult(3UL) { LastScannedEventSequenceNumber = 7UL }),
            options);
    }

    async Task Because() => await _job.Start(_request);

    [Fact] void should_report_the_partition_as_recovered() => _observer.Received(1).FailedPartitionRecovered((Key)"some-partition", (EventSequenceNumber)3UL, (EventSequenceNumber)7UL);
    [Fact] void should_not_report_a_partial_recovery() => _observer.DidNotReceive().FailedPartitionPartiallyRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}
