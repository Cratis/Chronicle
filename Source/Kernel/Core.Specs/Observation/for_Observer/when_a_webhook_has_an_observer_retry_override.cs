// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A webhook keeps its own retry policy, even when an observer retry override exists for its key.
/// </summary>
public class when_a_webhook_has_an_observer_retry_override : given.a_webhook_observer
{
    FailedPartition _failure;
    double _firstDelay;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0 };
    protected override Observers CreateWebhookRetryConfig() => new Configuration.Webhooks().Retry;

    void Establish()
    {
        var monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        monitor.CurrentValue.Returns(new ChronicleOptions
        {
            ObserverRetryOverrides =
            {
                [_observerKey.ToString()] = new ObserverRetryOverride { MaxRetryAttempts = 1, BackoffDelay = 9999 }
            }
        });
        var provider = new ConfigurationForObserverProvider(monitor);
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(call => provider.GetFor(call.Arg<string>()));
    }

    async Task Because()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling, _failure?.Id, _failure?.Deferral?.Id);
            _failure = _failedPartitionsState.Partitions.Single();
            if (attempt == 0)
            {
                _firstDelay = (_failure.Deferral.Until - _failure.Deferral.Since).TotalSeconds;
            }
        }
    }

    [Fact] void should_not_quarantine_the_partition() => _failure.IsQuarantined.ShouldBeFalse();
    [Fact] void should_use_the_webhook_schedule_for_the_first_delay() => (_firstDelay >= 0.8 && _firstDelay <= 1.2).ShouldBeTrue();
}
