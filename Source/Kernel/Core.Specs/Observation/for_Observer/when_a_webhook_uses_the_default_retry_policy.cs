// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_uses_the_default_retry_policy : given.a_webhook_observer
{
    readonly List<double> _delays = [];
    readonly double[] _expectedDelays = [1, 5, 30, 120, 600, 1800, 3600];
    FailedPartition _failure;

    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 1, BackoffDelay = 9999 };
    protected override Configuration.Observers CreateWebhookRetryConfig() => new Configuration.Webhooks().Retry;

    async Task Because()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling, _failure?.Id, _failure?.Deferral?.Id);
            _failure = _failedPartitionsState.Partitions.Single();
            _delays.Add((_failure.Deferral!.Until - _failure.Deferral.Since).TotalSeconds);
        }
    }

    [Fact] void should_use_the_webhook_schedule_with_bounded_jitter() => _delays.Zip(_expectedDelays, (delay, expected) => delay >= expected * 0.8 && delay <= Math.Min(expected * 1.2, 3600)).All(inRange => inRange).ShouldBeTrue();
    [Fact] void should_continue_hourly_after_the_last_schedule_entry() => _delays.Skip(7).All(delay => delay >= 2880 && delay <= 3600).ShouldBeTrue();
    [Fact] void should_not_use_the_global_attempt_limit() => _failure.IsQuarantined.ShouldBeFalse();
    [Fact] void should_record_every_actual_attempt() => _failure.AttemptsInCurrentBudget.ShouldEqual(12);
}
