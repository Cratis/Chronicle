// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_retry_policy_is_bound_from_configuration : given.a_webhook_observer
{
    FailedPartition _failure;
    double _firstDelay;

    protected override Configuration.Observers CreateWebhookRetryConfig() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Webhooks:Retry:BackoffScheduleSeconds:0"] = "7",
            ["Cratis:Chronicle:Webhooks:Retry:MaximumBackoffDelay"] = "8",
            ["Cratis:Chronicle:Webhooks:Retry:MaxRetryAttempts"] = "1",
            ["Cratis:Chronicle:Webhooks:Retry:RetryBudget"] = "01:00:00",
            ["Cratis:Chronicle:Webhooks:Retry:QuarantineOnFailedPartitionCount"] = "0",
            ["Cratis:Chronicle:Webhooks:Retry:QuarantineOnFailedPartitionPercentage"] = "0"
        })
        .Build().GetSection("Cratis:Chronicle:Webhooks:Retry").Get<Configuration.Observers>()!;

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling);
        _failure = _failedPartitionsState.Partitions.Single();
        _firstDelay = (_failure.Deferral!.Until - _failure.Deferral.Since).TotalSeconds;
        await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling, _failure.Id, _failure.Deferral.Id);
    }

    [Fact] void should_bind_the_schedule_and_cap() => _firstDelay.ShouldBeInRange(5.6, 8d);
    [Fact] void should_honor_the_attempt_limit() => _failure.IsQuarantined.ShouldBeTrue();
    [Fact] void should_honor_disabled_observer_quarantine() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
}
