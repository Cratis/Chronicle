// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the failed-partition retry settings of a single observer that override the global <see cref="Observers"/> settings.
/// </summary>
/// <remarks>
/// A setting that is not configured is <c language="csharp">null</c> and inherits the value from the global
/// <see cref="Observers"/> configuration.
/// </remarks>
public class ObserverRetryOverride
{
    /// <summary>
    /// Gets the maximum number of retry attempts for a failed partition, where zero means unlimited.
    /// When null, the global <see cref="Observers.MaxRetryAttempts"/> is used.
    /// </summary>
    public int? MaxRetryAttempts { get; init; }

    /// <summary>
    /// Gets the base backoff delay in seconds.
    /// When null, the global <see cref="Observers.BackoffDelay"/> is used.
    /// </summary>
    public int? BackoffDelay { get; init; }

    /// <summary>
    /// Gets the factor the backoff delay grows with for every attempt.
    /// When null, the global <see cref="Observers.ExponentialBackoffDelayFactor"/> is used.
    /// </summary>
    public float? ExponentialBackoffDelayFactor { get; init; }

    /// <summary>
    /// Gets the maximum backoff delay in seconds.
    /// When null, the global <see cref="Observers.MaximumBackoffDelay"/> is used.
    /// </summary>
    public int? MaximumBackoffDelay { get; init; }

    /// <summary>
    /// Applies the override on top of the global observer settings.
    /// </summary>
    /// <param name="observers">The global <see cref="Observers"/> settings.</param>
    /// <returns>A new <see cref="Observers"/> instance with the configured retry settings replaced.</returns>
    public Observers ApplyTo(Observers observers) => new()
    {
        MaxConcurrentPartitions = observers.MaxConcurrentPartitions,
        SubscriberTimeout = observers.SubscriberTimeout,
        LiveDeliveryBudget = observers.LiveDeliveryBudget,
        AbandonedDeliveryHold = observers.AbandonedDeliveryHold,
        MaxEventsPerSubscriberCall = observers.MaxEventsPerSubscriberCall,
        SubscriptionReadyTimeout = observers.SubscriptionReadyTimeout,
        MaxRetryAttempts = MaxRetryAttempts ?? observers.MaxRetryAttempts,
        BackoffDelay = BackoffDelay ?? observers.BackoffDelay,
        ExponentialBackoffDelayFactor = ExponentialBackoffDelayFactor ?? observers.ExponentialBackoffDelayFactor,
        MaximumBackoffDelay = MaximumBackoffDelay ?? observers.MaximumBackoffDelay,
        BackoffScheduleSeconds = observers.BackoffScheduleSeconds,
        RetryBudget = observers.RetryBudget,
        QuarantineOnFailedPartitionCount = observers.QuarantineOnFailedPartitionCount,
        QuarantineOnFailedPartitionPercentage = observers.QuarantineOnFailedPartitionPercentage,
        DefinitionEvolution = observers.DefinitionEvolution,
        GenerationPromotion = observers.GenerationPromotion,
        DeploymentConditionGracePeriod = observers.DeploymentConditionGracePeriod,
        PendingGenerationAlertAfter = observers.PendingGenerationAlertAfter,
        ReplayOnDefinitionChange = observers.ReplayOnDefinitionChange,
        WatchdogInterval = observers.WatchdogInterval,
        StatePersistenceBatchInterval = observers.StatePersistenceBatchInterval,
        WriteCoalescingInterval = observers.WriteCoalescingInterval,
        FanOutStrategy = observers.FanOutStrategy,
        MaxCatchupRecoveryAttempts = observers.MaxCatchupRecoveryAttempts
    };

    /// <summary>
    /// Validates the override and the key it is configured for.
    /// </summary>
    /// <param name="observerKey">The key the override is configured for.</param>
    /// <returns>The validation problems, empty when the override is valid.</returns>
    public IEnumerable<ValidationResult> Validate(string observerKey)
    {
        var member = $"{nameof(ChronicleOptions.ObserverRetryOverrides)}[{observerKey}]";
        var parts = observerKey.Split(KeyHelper.Separator);

        if (parts.Length != 4 || parts.Any(string.IsNullOrWhiteSpace))
        {
            yield return new ValidationResult(
                $"ObserverRetryOverrides['{observerKey}'] must be a full observer key '<observer id>#<event store>#<namespace>#<event sequence id>'.",
                [member]);
        }

        if (MaxRetryAttempts < 0)
        {
            yield return new ValidationResult(
                $"ObserverRetryOverrides['{observerKey}'].MaxRetryAttempts must be non-negative; zero means unlimited.",
                [$"{member}.{nameof(MaxRetryAttempts)}"]);
        }

        if (BackoffDelay <= 0)
        {
            yield return new ValidationResult(
                $"ObserverRetryOverrides['{observerKey}'].BackoffDelay must be greater than zero.",
                [$"{member}.{nameof(BackoffDelay)}"]);
        }

        if (MaximumBackoffDelay <= 0)
        {
            yield return new ValidationResult(
                $"ObserverRetryOverrides['{observerKey}'].MaximumBackoffDelay must be greater than zero.",
                [$"{member}.{nameof(MaximumBackoffDelay)}"]);
        }

        if (ExponentialBackoffDelayFactor is { } factor && (!float.IsFinite(factor) || factor < 1))
        {
            yield return new ValidationResult(
                $"ObserverRetryOverrides['{observerKey}'].ExponentialBackoffDelayFactor must be a finite number of at least one.",
                [$"{member}.{nameof(ExponentialBackoffDelayFactor)}"]);
        }
    }
}
