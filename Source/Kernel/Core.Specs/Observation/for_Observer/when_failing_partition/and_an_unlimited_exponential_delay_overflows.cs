// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Options;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_an_unlimited_exponential_delay_overflows : given.an_observer
{
    Exception? _error;

    void Establish()
    {
        var monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        monitor.CurrentValue.Returns(new ChronicleOptions
        {
            Observers = _observersConfig,
            ObserverRetryOverrides = new Dictionary<string, ObserverRetryOverrides>
            {
                [_observerKey.ToString()] = new() { MaxRetryAttempts = 0, BackoffDelay = 1, ExponentialBackoffDelayFactor = float.MaxValue, MaximumBackoffDelay = 10 }
            }
        });
        var provider = new ConfigurationForObserverProvider(monitor);
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(call => provider.GetFor(call.Arg<string>()));
        for (var attempt = 0; attempt < 9; attempt++)
        {
            _failedPartitionsState.RegisterAttempt("partition", 42UL, ["Failed"], "Stack", FailureKind.Handling);
        }
    }

    async Task Because() => _error = await Catch.Exception(() => _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack", FailureKind.Handling));

    [Fact] void should_not_throw_on_positive_infinity() => _error.ShouldBeNull();
    [Fact] void should_record_the_tenth_attempt() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(10);
    [Fact] void should_not_quarantine() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
    [Fact] void should_schedule_at_the_cap_with_the_minimum_period() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id), TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1)), Times.Once);
}
