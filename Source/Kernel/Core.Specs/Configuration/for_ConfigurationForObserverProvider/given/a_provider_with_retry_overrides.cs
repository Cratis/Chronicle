// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider.given;

public class a_provider_with_retry_overrides : Specification
{
    protected const string FullKey = "full#my-store#Default#event-log";
    protected const string PartialKey = "partial#my-store#Default#event-log";

    protected Observers _global;
    protected ConfigurationForObserverProvider _provider;

    void Establish()
    {
        _global = new Observers
        {
            MaxConcurrentPartitions = 7,
            SubscriberTimeout = 11,
            LiveDeliveryBudget = 47,
            AbandonedDeliveryHold = 13,
            MaxEventsPerSubscriberCall = 17,
            SubscriptionReadyTimeout = 19,
            MaxRetryAttempts = 23,
            BackoffDelay = 3,
            ExponentialBackoffDelayFactor = 1.5f,
            MaximumBackoffDelay = 900,
            BackoffScheduleSeconds = [2, 4],
            RetryBudget = TimeSpan.FromHours(2),
            QuarantineOnFailedPartitionCount = 29,
            QuarantineOnFailedPartitionPercentage = 0.25,
            DefinitionEvolution = DefinitionEvolutionPolicy.Manual,
            GenerationPromotion = PromotionGateMode.Operator,
            DeploymentConditionGracePeriod = TimeSpan.FromSeconds(90),
            PendingGenerationAlertAfter = TimeSpan.FromHours(48),
            ReplayOnDefinitionChange = true,
            WatchdogInterval = 31,
            StatePersistenceBatchInterval = 37,
            WriteCoalescingInterval = 41,
            FanOutStrategy = "random",
            MaxCatchupRecoveryAttempts = 43
        };

        var monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        monitor.CurrentValue.Returns(new ChronicleOptions
        {
            Observers = _global,
            ObserverRetryOverrides =
            {
                [FullKey] = new ObserverRetryOverride
                {
                    MaxRetryAttempts = 2,
                    BackoffDelay = 5,
                    ExponentialBackoffDelayFactor = 3,
                    MaximumBackoffDelay = 120
                },
                [PartialKey] = new ObserverRetryOverride { MaxRetryAttempts = 2 }
            }
        });
        _provider = new ConfigurationForObserverProvider(monitor);
    }
}
