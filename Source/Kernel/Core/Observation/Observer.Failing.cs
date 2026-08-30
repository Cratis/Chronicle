// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task PartitionFailed(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind = FailureKind.Unknown)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        _metrics?.PartitionFailed(partition);
        logger.PartitionFailed(partition, sequenceNumber, exceptionMessages, exceptionStackTrace);
        var partitionWasAlreadyFailed = Failures.IsFailed(partition);
        var failure = failures.State.RegisterAttempt(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind);
        if (!partitionWasAlreadyFailed)
        {
            State = State with { FailedPartitionCount = State.FailedPartitionCount + 1 };
        }

        _metrics?.PartitionRetryAttempt(partition);
        var config = await configurationProvider.GetFor(_observerKey);
        if (State.RunningState == ObserverRunningState.Quarantined)
        {
            await failures.WriteStateAsync();
            return;
        }

        if (ShouldQuarantineObserver(config))
        {
            await TransitionTo<QuarantinedObserver>();
        }

        var attemptCount = failure.Attempts.Count();
        if (State.RunningState == ObserverRunningState.Quarantined)
        {
            await failures.WriteStateAsync();
            return;
        }

        if (config.MaxRetryAttempts == 0 || attemptCount <= config.MaxRetryAttempts)
        {
            await this.RegisterOrUpdateReminder(partition.ToString(), GetNextRetryDelay(failure, config), TimeSpan.FromHours(48));
        }
        else
        {
            logger.QuarantiningFailedPartition(partition);
            failures.State.Quarantine(partition);
            _metrics?.PartitionQuarantined(partition);
        }

        await failures.WriteStateAsync();
        if (!partitionWasAlreadyFailed)
        {
            await WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public async Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FailingPartitionRecovered(partition);
        var partitionWasFailed = Failures.IsFailed(partition);
        failures.State.Remove(partition);
        await failures.WriteStateAsync();
        if (partitionWasFailed)
        {
            State = State with { FailedPartitionCount = State.FailedPartitionCount - 1 };
        }

        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, lastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public async Task FailedPartitionPartiallyRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FailingPartitionPartiallyRecovered(partition, lastHandledEventSequenceNumber);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task TryStartRecoverJobForFailedPartition(Key partition)
    {
        if (State.RunningState == ObserverRunningState.Quarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsQuarantined();
            return;
        }

        if (!Failures.TryGet(partition, out var failure))
        {
            return;
        }

        await StartRecoverJobForFailedPartition(failure);
    }

    /// <inheritdoc/>
    public async Task TryRecoverAllFailedPartitions()
    {
        if (State.RunningState == ObserverRunningState.Quarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsQuarantined();
            return;
        }

        var config = await configurationProvider.GetFor(_observerKey);
        foreach (var partition in Failures.Partitions.Where(p => !p.IsQuarantined))
        {
            var attemptCount = partition.Attempts.Count();
            if (config.MaxRetryAttempts > 0 && attemptCount > config.MaxRetryAttempts)
            {
                logger.SkippingRecoveryMaxAttemptsExceeded(partition.Partition, attemptCount, config.MaxRetryAttempts);
                continue;
            }

            if (attemptCount > 0)
            {
                logger.StartingRecoveryWithExistingAttempts(partition.Partition, attemptCount, config.MaxRetryAttempts);
            }

            await StartRecoverJobForFailedPartition(partition);
        }
    }

    static TimeSpan GetNextRetryDelay(FailedPartition failure, Observers config)
    {
        var time = TimeSpan.FromSeconds(config.BackoffDelay * Math.Pow(config.ExponentialBackoffDelayFactor, failure.Attempts.Count()));
        var maxTime = TimeSpan.FromSeconds(config.MaximumBackoffDelay);

        if (time > maxTime)
        {
            return maxTime;
        }

        if (time == TimeSpan.Zero)
        {
            return TimeSpan.FromSeconds(config.BackoffDelay);
        }

        return time;
    }

    async Task StartRecoverJobForFailedPartition(FailedPartition failedPartition)
    {
        if (State.RunningState == ObserverRunningState.Quarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsQuarantined();
            return;
        }

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.TryingToRecoverFailedPartition(failedPartition.Partition);
        var request = new RetryFailedPartitionRequest(_observerKey, Definition.Type, failedPartition.Partition, failedPartition.LastAttempt.SequenceNumber, Definition.EventTypes);
        await _jobsManager.StartOrResumeObserverJobFor<IRetryFailedPartition, RetryFailedPartitionRequest>(
            logger,
            request,
            requestPredicate: r => r.Key == failedPartition.Partition);
    }

    /// <summary>
    /// Check whether the observer's failures have reached a threshold that takes it out of service.
    /// </summary>
    /// <param name="config">The <see cref="Observers"/> configuration holding the thresholds.</param>
    /// <returns>True when the observer should be quarantined, false if not.</returns>
    /// <remarks>
    /// A partition whose last attempt timed out does not count toward either threshold. Quarantining stops retries
    /// and needs an operator to undo, which is the right answer for an observer that is wrong and the wrong answer
    /// for one that is only waiting on a congested kernel - the congestion is transient and takes the retries with it
    /// when it clears. Counting timeouts would let a busy period take healthy projections out of service, and the
    /// operator who then cleared the quarantine would find nothing wrong with them.
    /// </remarks>
    bool ShouldQuarantineObserver(Observers config)
    {
        var failedPartitionCount = Failures.Partitions.Count(_ => _.LastAttempt.Kind != FailureKind.Timeout);
        if (config.QuarantineOnFailedPartitionCount > 0 && failedPartitionCount >= config.QuarantineOnFailedPartitionCount)
        {
            logger.ObserverFailedPartitionCountThresholdReached(failedPartitionCount, config.QuarantineOnFailedPartitionCount);
            return true;
        }

        if (config.QuarantineOnFailedPartitionPercentage > 0.0 && failedPartitionCount > 0)
        {
            var totalObservedPartitions = Failures.Partitions
                .Select(_ => _.Partition)
                .Concat(Failures.ResolvedPartitions.Select(_ => _.Partition))
                .Distinct()
                .Count();

            var failedPartitionRatio = totalObservedPartitions == 0
                ? 0.0
                : (double)failedPartitionCount / totalObservedPartitions;
            if (failedPartitionRatio > config.QuarantineOnFailedPartitionPercentage)
            {
                logger.ObserverFailedPartitionPercentageThresholdExceeded(failedPartitionRatio, config.QuarantineOnFailedPartitionPercentage);
                return true;
            }
        }

        return false;
    }
}
