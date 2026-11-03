// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_host_retry_overrides_are_configured : given.an_observer
{
    int _firstCount;
    int _secondCount;
    bool _firstQuarantined;
    bool _secondQuarantined;

    void Establish()
    {
        var monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        monitor.CurrentValue.Returns(new ChronicleOptions
        {
            Observers = _observersConfig,
            ObserverRetryOverrides = new Dictionary<string, ObserverRetryOverrides>
            {
                [_observerKey.ToString()] = new() { MaxRetryAttempts = 2, BackoffDelay = 5, ExponentialBackoffDelayFactor = 3, MaximumBackoffDelay = 20 }
            }
        });
        var provider = new ConfigurationForObserverProvider(monitor);
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(call => provider.GetFor(call.Arg<string>()));
    }

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");
        _firstCount = _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget;
        _firstQuarantined = _failedPartitionsState.Partitions.Single().IsQuarantined;
        await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");
        _secondCount = _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget;
        _secondQuarantined = _failedPartitionsState.Partitions.Single().IsQuarantined;
        await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");
    }

    [Fact] void should_keep_the_first_two_failures_in_budget() => (_firstCount, _secondCount, _firstQuarantined, _secondQuarantined).ShouldEqual((1, 2, false, false));
    [Fact] void should_use_exponent_one_on_the_first_failure() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id), TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1)), Times.Once);
    [Fact] void should_cap_the_second_delay() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id), TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(1)), Times.Once);
    [Fact] void should_not_schedule_a_third_retry() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Exactly(2));
    [Fact] void should_quarantine_the_third_same_position_failure() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_retain_the_third_attempt() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(3);
    [Fact] void should_leave_global_retry_settings_unchanged() => (_observersConfig.MaxRetryAttempts, _observersConfig.BackoffDelay, _observersConfig.ExponentialBackoffDelayFactor, _observersConfig.MaximumBackoffDelay).ShouldEqual((10, 1, 2f, 600));
}
