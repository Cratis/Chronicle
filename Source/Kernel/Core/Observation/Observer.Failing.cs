// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Jobs;
namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task PartitionFailed(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        _metrics?.PartitionFailed(partition);
        logger.PartitionFailed(partition, sequenceNumber, exceptionMessages, exceptionStackTrace);
        var failure = failures.State.RegisterAttempt(partition, sequenceNumber, exceptionMessages, exceptionStackTrace);
        _metrics?.PartitionRetryAttempt(partition);
        var config = await configurationProvider.GetFor(_observerKey);
        var attemptCount = failure.Attempts.Count();
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
    }

    /// <inheritdoc/>
    public async Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FailingPartitionRecovered(partition);
        failures.State.Remove(partition);
        await failures.WriteStateAsync();
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
        if (!Failures.TryGet(partition, out var failure))
        {
            return;
        }

        await StartRecoverJobForFailedPartition(failure);
    }

    /// <inheritdoc/>
    public async Task TryRecoverAllFailedPartitions()
    {
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
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.TryingToRecoverFailedPartition(failedPartition.Partition);
        var request = new RetryFailedPartitionRequest(_observerKey, Definition.Type, failedPartition.Partition, failedPartition.LastAttempt.SequenceNumber, Definition.EventTypes);
        await _jobsManager.StartOrResumeObserverJobFor<IRetryFailedPartition, RetryFailedPartitionRequest>(
            logger,
            request,
            requestPredicate: r => r.Key == failedPartition.Partition);
    }
}
