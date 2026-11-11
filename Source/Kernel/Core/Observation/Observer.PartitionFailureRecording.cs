// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Identifies a failed delivery, its recovery attempt and its delivery context.
    /// </summary>
    readonly record struct PartitionFailure(
        Key Partition,
        EventSequenceNumber SequenceNumber,
        IEnumerable<string> ExceptionMessages,
        string ExceptionStackTrace,
        FailureKind Kind,
        FailedPartitionId? EpisodeId,
        Guid? DeferralId,
        PartitionFailureContext Context);

    /// <summary>
    /// The quarantine and retry decisions for a recorded failure.
    /// </summary>
    readonly record struct PartitionFailureOutcome(bool QuarantineObserver, TimeSpan? RetryDelay);

    /// <summary>
    /// Registers and logs an attempt without changing its delivery fence or failure count.
    /// </summary>
    /// <param name="request">The failed delivery.</param>
    /// <returns>The failure and whether the partition was already failed.</returns>
    (FailedPartition Failure, bool WasAlreadyFailed) RegisterFailureAttempt(PartitionFailure request)
    {
        var (partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind, _, _, context) = request;
        _metrics?.PartitionFailed();
        var partitionWasAlreadyFailed = Failures.IsFailed(partition);
        var failure = failures.State.RegisterAttempt(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind);

        // The epoch is an identity tag: a failure from a non-replay delivery never clears it.
        if (context.ReplayEpoch is { } replayEpoch) failure.ReplayEpoch = replayEpoch;
        if (partitionWasAlreadyFailed)
        {
            logger.PartitionFailedAgain(partition, sequenceNumber, failure.AttemptsInCurrentBudget);
        }
        else
        {
            logger.PartitionFailed(partition, sequenceNumber, FailureKindCode(kind));
        }
        logger.PartitionFailureDetails(partition, exceptionMessages, exceptionStackTrace);
        return (failure, partitionWasAlreadyFailed);
    }

    /// <summary>
    /// Decides whether to quarantine the observer or retry the failed partition.
    /// </summary>
    /// <param name="failure">The recorded failure.</param>
    /// <param name="config">The retry configuration.</param>
    /// <param name="webhook">Whether the subscription is a webhook.</param>
    /// <param name="request">The failed delivery.</param>
    /// <returns>The quarantine and retry decisions.</returns>
    PartitionFailureOutcome DecideQuarantineAndRetry(FailedPartition failure, Observers config, bool webhook, PartitionFailure request)
    {
        var quarantineObserver = false;
        TimeSpan? retryDelayToRegister = null;
        var partition = request.Partition;
        var context = request.Context;
        var sequenceNumber = request.SequenceNumber;
        if (!IsQuarantined)
        {
            quarantineObserver = webhook ? context.Policy == WebhookDeliveryFailurePolicy.PauseObserver : ShouldQuarantineObserver(config);
            if (webhook && context.Policy == WebhookDeliveryFailurePolicy.ParkPartition)
            {
                QuarantinePartition(partition);
            }
            else if (!quarantineObserver)
            {
                retryDelayToRegister = DecidePartitionRetry(failure, config, webhook, sequenceNumber);
            }
        }

        if (webhook && !quarantineObserver)
        {
            quarantineObserver = ShouldQuarantineObserver(config);
        }
        return new(quarantineObserver, retryDelayToRegister);
    }

    /// <summary>
    /// Selects retry backoff or quarantines a partition whose attempts are exhausted.
    /// </summary>
    /// <param name="failure">The failure being retried.</param>
    /// <param name="config">The retry configuration.</param>
    /// <param name="webhook">Whether the subscription is a webhook.</param>
    /// <param name="sequenceNumber">The failed event.</param>
    /// <returns>The retry delay, if any.</returns>
    TimeSpan? DecidePartitionRetry(FailedPartition failure, Observers config, bool webhook, EventSequenceNumber sequenceNumber)
    {
        if (RetriesRemain(failure, config, webhook))
        {
            return webhook ? SetWebhookRetryBackoff(failure, config, sequenceNumber) : GetNextRetryDelay(failure, config);
        }
        else
        {
            QuarantinePartition(failure.Partition);
        }
        return null;
    }

    /// <summary>
    /// Checks attempt and webhook retry-budget limits in their original order.
    /// </summary>
    /// <param name="failure">The recorded failure.</param>
    /// <param name="config">The retry configuration.</param>
    /// <param name="webhook">Whether the subscription is a webhook.</param>
    /// <returns>Whether another retry is permitted.</returns>
    bool RetriesRemain(FailedPartition failure, Observers config, bool webhook) =>
        (config.MaxRetryAttempts == 0 || failure.AttemptsInCurrentBudget <= config.MaxRetryAttempts) &&
        (!webhook || GetRemainingRetryBudget(failure, config) > TimeSpan.Zero);

    /// <summary>
    /// Quarantines a failed partition and records the existing log and metric.
    /// </summary>
    /// <param name="partition">The failed partition.</param>
    void QuarantinePartition(Key partition)
    {
        logger.QuarantiningFailedPartition(partition);
        failures.State.Quarantine(partition);
        _metrics?.PartitionQuarantined();
    }

    /// <summary>
    /// Persists a failure immediately or queues its observer-state and reminder writes.
    /// </summary>
    /// <param name="partition">The failed partition.</param>
    /// <param name="failure">The recorded episode.</param>
    /// <param name="retryDelayToRegister">The reminder delay, if any.</param>
    /// <returns>Awaitable task.</returns>
    async Task PersistFailure(Key partition, FailedPartition failure, TimeSpan? retryDelayToRegister)
    {
        if (_writeCoalescingInterval > TimeSpan.Zero)
        {
            // Only this partition is written now, and only once: rewriting every failed partition per failure is
            // what made a burst of failures cost O(n) writes each. The reminder and the observer state, which a
            // burst of failures has in common, are written together by the flush.
            await SaveFailedPartition(partition);
            if (retryDelayToRegister is { } pendingDelay)
            {
                _pendingRetryReminders[failure.Id] = pendingDelay;
            }

            _failureStateWritePending = true;
            EnsureCoalescedWriteTimer();
        }
        else
        {
            if (retryDelayToRegister is { } retryDelay)
            {
                await this.RegisterOrUpdateReminder(PartitionReminderName(failure.Id), retryDelay, GetRetryReminderPeriod(retryDelay));
            }

            // Fence old reports before the partition write. A crash between the two writes reports whichever
            // partition level actually committed. Reports alone are coalesced, never these source writes.
            await WriteStateAsync();
            await WriteFailedPartitions();
        }
    }
}
