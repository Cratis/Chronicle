// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Options;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_has_host_observer_retry_overrides : given.a_webhook_observer
{
    protected override Observers CreateWebhookRetryConfig() => new()
    {
        MaxRetryAttempts = 0,
        BackoffScheduleSeconds = [25],
        MaximumBackoffDelay = 25,
        RetryBudget = TimeSpan.FromHours(1)
    };

    void Establish()
    {
        var monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        monitor.CurrentValue.Returns(new ChronicleOptions
        {
            Observers = _observersConfig,
            ObserverRetryOverrides = new Dictionary<string, ObserverRetryOverrides>
            {
                [_observerKey.ToString()] = new() { MaxRetryAttempts = 1, BackoffDelay = 1, ExponentialBackoffDelayFactor = 1, MaximumBackoffDelay = 1 }
            }
        });
        var provider = new ConfigurationForObserverProvider(monitor);
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(call => provider.GetFor(call.Arg<string>()));
        for (var attempt = 0; attempt < 3; attempt++)
        {
            _failedPartitionsState.RegisterAttempt("partition", 42UL, ["503"], "", FailureKind.Handling);
        }
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling);

    [Fact] void should_use_the_webhook_budget_instead_of_the_host_limit() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
    [Fact] void should_record_the_fourth_attempt() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(4);
    [Fact] void should_use_the_webhook_schedule_and_jitter() => (_failedPartitionsState.Partitions.Single().Deferral!.Until - _failedPartitionsState.Partitions.Single().Deferral!.Since).TotalSeconds.ShouldBeInRange(20d, 25d);
    [Fact] void should_create_a_retry_backoff_deferral() => _failedPartitionsState.Partitions.Single().Deferral!.Reason.ShouldEqual(PartitionDeferralReason.RetryBackoff);
    [Fact] void should_register_the_webhook_retry_not_the_host_delay() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.Is<TimeSpan>(delay => delay.TotalSeconds > 1 && delay.TotalSeconds <= 25), TimeSpan.FromMinutes(1)), Times.Once);
}
