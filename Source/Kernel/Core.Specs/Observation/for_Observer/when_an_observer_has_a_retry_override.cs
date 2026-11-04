// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// An observer with a retry override uses it, through the configuration provider, instead of the global retry policy.
/// </summary>
public class when_an_observer_has_a_retry_override : given.an_observer
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0 };

    void Establish()
    {
        var monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        monitor.CurrentValue.Returns(new ChronicleOptions
        {
            Observers = _observersConfig,
            ObserverRetryOverrides =
            {
                [_observerKey.ToString()] = new ObserverRetryOverride
                {
                    MaxRetryAttempts = 2,
                    BackoffDelay = 7,
                    ExponentialBackoffDelayFactor = 1,
                    MaximumBackoffDelay = 100
                }
            }
        });
        var provider = new ConfigurationForObserverProvider(monitor);
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(call => provider.GetFor(call.Arg<string>()));
    }

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["failure"], "");
        await _observer.PartitionFailed("partition", 42UL, ["failure"], "");
        await _observer.PartitionFailed("partition", 42UL, ["failure"], "");
    }

    [Fact] void should_use_the_overridden_backoff_delay() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), TimeSpan.FromSeconds(7), TimeSpan.FromMinutes(1)), Times.Exactly(2));
    [Fact] void should_quarantine_the_partition_after_the_overridden_attempt_limit() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeTrue();
}
