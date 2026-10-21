// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_uses_a_configured_elapsed_retry_budget : given.a_webhook_observer
{
    FailedPartition _failure;

    protected override Configuration.Observers CreateWebhookRetryConfig() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Webhooks:Retry:RetryBudget"] = "01:00:00",
            ["Webhooks:Retry:MaxRetryAttempts"] = "0"
        }).Build().GetSection("Webhooks:Retry").Get<Configuration.Observers>()!;

    void Establish()
    {
        _failure = _failedPartitionsState.RegisterAttempt("partition", 42UL, ["503"], "", FailureKind.Handling);
        _failure.LastAttempt.Occurred = DateTimeOffset.UtcNow.AddHours(-2);
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling, _failure.Id);

    [Fact] void should_park_at_the_configured_budget_instead_of_waiting_twenty_four_hours() => _failure.IsQuarantined.ShouldBeTrue();
}
