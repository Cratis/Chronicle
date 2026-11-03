// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ConfigurationForObserverProvider.given;

public class a_provider : Specification
{
    protected const string ObserverKey = "observer#store#namespace#sequence";
    protected IOptionsMonitor<ChronicleOptions> _monitor;
    protected ConfigurationForObserverProvider _provider;
    protected Observers _defaults;
    protected Observers _result;

    void Establish()
    {
        _defaults = new Observers
        {
            MaxConcurrentPartitions = 7,
            SubscriberTimeout = 43,
            AbandonedDeliveryHold = 401,
            MaxEventsPerSubscriberCall = 17,
            SubscriptionReadyTimeout = 13,
            MaxRetryAttempts = 8,
            BackoffDelay = 4,
            ExponentialBackoffDelayFactor = 3,
            MaximumBackoffDelay = 90,
            BackoffScheduleSeconds = [11, 23],
            RetryBudget = TimeSpan.FromMinutes(13),
            QuarantineOnFailedPartitionCount = 6,
            QuarantineOnFailedPartitionPercentage = 0.7,
            DefinitionEvolution = DefinitionEvolutionPolicy.Manual,
            GenerationPromotion = PromotionGateMode.Operator,
            DeploymentConditionGracePeriod = TimeSpan.FromSeconds(17),
            PendingGenerationAlertAfter = TimeSpan.FromHours(3),
            ReplayOnDefinitionChange = true,
            WatchdogInterval = 29,
            StatePersistenceBatchInterval = 31,
            WriteCoalescingInterval = 33,
            FanOutStrategy = "random",
            MaxCatchupRecoveryAttempts = 9
        };
        _monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        _monitor.CurrentValue.Returns(new ChronicleOptions { Observers = _defaults });
        _provider = new(_monitor);
    }

    protected void Configure(IDictionary<string, ObserverRetryOverrides> overrides) =>
        _monitor.CurrentValue.Returns(new ChronicleOptions { Observers = _defaults, ObserverRetryOverrides = overrides });

    protected void AssertUnrelatedPropertiesArePreserved()
    {
        string[] retryProperties = [nameof(Observers.MaxRetryAttempts), nameof(Observers.BackoffDelay), nameof(Observers.ExponentialBackoffDelayFactor), nameof(Observers.MaximumBackoffDelay)];
        foreach (var property in typeof(Observers).GetProperties().Where(_ => !retryProperties.Contains(_.Name)))
        {
            property.GetValue(_result).ShouldEqual(property.GetValue(_defaults));
        }
    }
}
