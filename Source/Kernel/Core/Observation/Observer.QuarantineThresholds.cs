// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Check whether the observer's failures have reached a threshold that takes it out of service.
    /// </summary>
    /// <param name="config">The <see cref="Observers"/> configuration holding the thresholds.</param>
    /// <returns>True when the observer should be quarantined, false if not.</returns>
    /// <remarks>
    /// For non-webhook observers, a partition whose last attempt timed out does not count toward either threshold. Quarantining stops retries
    /// and needs an operator to undo, which is the right answer for an observer that is wrong and the wrong answer
    /// for one that is only waiting on a congested kernel - the congestion is transient and takes the retries with it
    /// when it clears. Counting timeouts would let a busy period take healthy projections out of service, and the
    /// operator who then cleared the quarantine would find nothing wrong with them.
    /// </remarks>
    bool ShouldQuarantineObserver(Observers config)
    {
        var webhook = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType);
        var failedPartitionCount = Failures.Partitions.Count(_ => CountsTowardQuarantine(_, webhook));
        if (QuarantineCountThresholdReached(failedPartitionCount, config)) return true;
        return QuarantinePercentageThresholdReached(failedPartitionCount, config, webhook);
    }

    /// <summary>
    /// Selects failures counted by the subscription's quarantine policy.
    /// </summary>
    /// <param name="failure">The failed partition.</param>
    /// <param name="webhook">Whether the subscription is a webhook.</param>
    /// <returns>Whether this failure counts toward the thresholds.</returns>
    static bool CountsTowardQuarantine(FailedPartition failure, bool webhook) =>
        webhook ? failure.IsQuarantined : failure.Attempts.Any() && failure.LastAttempt.Kind != FailureKind.Timeout;

    /// <summary>
    /// Checks and logs the absolute failure threshold.
    /// </summary>
    /// <param name="failedPartitionCount">The failures counted by the policy.</param>
    /// <param name="config">The threshold configuration.</param>
    /// <returns>Whether the count threshold is reached.</returns>
    bool QuarantineCountThresholdReached(int failedPartitionCount, Observers config)
    {
        if (config.QuarantineOnFailedPartitionCount > 0 && failedPartitionCount >= config.QuarantineOnFailedPartitionCount)
        {
            logger.ObserverFailedPartitionCountThresholdReached(failedPartitionCount, config.QuarantineOnFailedPartitionCount);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks and logs the percentage threshold, including each policy's exact boundary.
    /// </summary>
    /// <param name="failedPartitionCount">The failures counted by the policy.</param>
    /// <param name="config">The threshold configuration.</param>
    /// <param name="webhook">Whether the subscription is a webhook.</param>
    /// <returns>Whether the percentage threshold is reached.</returns>
    bool QuarantinePercentageThresholdReached(int failedPartitionCount, Observers config, bool webhook)
    {
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
            if (webhook ? failedPartitionRatio >= config.QuarantineOnFailedPartitionPercentage : failedPartitionRatio > config.QuarantineOnFailedPartitionPercentage)
            {
                logger.ObserverFailedPartitionPercentageThresholdExceeded(failedPartitionRatio, config.QuarantineOnFailedPartitionPercentage);
                return true;
            }
        }
        return false;
    }
}
