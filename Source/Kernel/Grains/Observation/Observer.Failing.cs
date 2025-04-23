// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Configuration;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;
namespace Cratis.Chronicle.Grains.Observation;

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
        logger.PartitionFailed(partition, sequenceNumber);
        var failure = failures.State.RegisterAttempt(partition, sequenceNumber, exceptionMessages, exceptionStackTrace);
        var config = await configurationProvider.GetFor(_observerKey);
        if (config.MaxRetryAttempts == 0 || failure.Attempts.Count() <= config.MaxRetryAttempts)
        {
            await this.RegisterOrUpdateReminder(partition.ToString(), GetNextRetryDelay(failure, config), TimeSpan.FromHours(48));
        }
        else
        {
            logger.GivingUpOnRecoveringFailedPartition(partition);
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
        foreach (var partition in Failures.Partitions)
        {
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
        await RemoveReminder(failedPartition.Partition.ToString());
        await _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(new(_observerKey, State.Type, failedPartition.Partition, failedPartition.LastAttempt.SequenceNumber, State.EventTypes));
    }
}
