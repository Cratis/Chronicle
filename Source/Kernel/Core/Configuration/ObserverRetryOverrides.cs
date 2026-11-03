// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents host retry overrides for one fully qualified non-webhook observer.
/// Null properties inherit the current global observer settings.
/// </summary>
public class ObserverRetryOverrides
{
    /// <summary>
    /// Gets the maximum retry attempts, or null to inherit. Zero means unlimited.
    /// </summary>
    public int? MaxRetryAttempts { get; init; }

    /// <summary>
    /// Gets the base delay in seconds, or null to inherit.
    /// </summary>
    public int? BackoffDelay { get; init; }

    /// <summary>
    /// Gets the exponential delay factor, or null to inherit.
    /// </summary>
    public float? ExponentialBackoffDelayFactor { get; init; }

    /// <summary>
    /// Gets the maximum delay in seconds, or null to inherit.
    /// </summary>
    public int? MaximumBackoffDelay { get; init; }

    internal Observers ApplyTo(Observers defaults) => new()
    {
        MaxConcurrentPartitions = defaults.MaxConcurrentPartitions,
        SubscriberTimeout = defaults.SubscriberTimeout,
        AbandonedDeliveryHold = defaults.AbandonedDeliveryHold,
        MaxEventsPerSubscriberCall = defaults.MaxEventsPerSubscriberCall,
        SubscriptionReadyTimeout = defaults.SubscriptionReadyTimeout,
        MaxRetryAttempts = MaxRetryAttempts ?? defaults.MaxRetryAttempts,
        BackoffDelay = BackoffDelay ?? defaults.BackoffDelay,
        ExponentialBackoffDelayFactor = ExponentialBackoffDelayFactor ?? defaults.ExponentialBackoffDelayFactor,
        MaximumBackoffDelay = MaximumBackoffDelay ?? defaults.MaximumBackoffDelay,
        BackoffScheduleSeconds = defaults.BackoffScheduleSeconds,
        RetryBudget = defaults.RetryBudget,
        QuarantineOnFailedPartitionCount = defaults.QuarantineOnFailedPartitionCount,
        QuarantineOnFailedPartitionPercentage = defaults.QuarantineOnFailedPartitionPercentage,
        DefinitionEvolution = defaults.DefinitionEvolution,
        GenerationPromotion = defaults.GenerationPromotion,
        DeploymentConditionGracePeriod = defaults.DeploymentConditionGracePeriod,
        PendingGenerationAlertAfter = defaults.PendingGenerationAlertAfter,
        ReplayOnDefinitionChange = defaults.ReplayOnDefinitionChange,
        WatchdogInterval = defaults.WatchdogInterval,
        StatePersistenceBatchInterval = defaults.StatePersistenceBatchInterval,
        WriteCoalescingInterval = defaults.WriteCoalescingInterval,
        FanOutStrategy = defaults.FanOutStrategy,
        MaxCatchupRecoveryAttempts = defaults.MaxCatchupRecoveryAttempts
    };
}
