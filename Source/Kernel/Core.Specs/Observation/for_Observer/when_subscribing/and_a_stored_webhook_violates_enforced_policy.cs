// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_stored_webhook_violates_enforced_policy : given.an_observer
{
    readonly given.RecordingObserverLogger _recordedLogs = new();

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    void Establish() => _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
        .Returns(new WebhookTargetValidation(false, "Webhook target must use HTTPS."));

    Task Because() => _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero);

    [Fact] void should_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_log_the_safe_rule_violation() => _recordedLogs.Warnings.Any(message => message.Contains("Webhook target must use HTTPS.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_attempt_delivery() => _subscriber.DidNotReceiveWithAnyArgs().OnNext(default!, default!, default!);
    [Fact] void should_not_start_catchup() => _jobsManager.DidNotReceiveWithAnyArgs().Resume(default!);
    [Fact] void should_not_create_partition_failures() => _failedPartitionsState.Partitions.ShouldBeEmpty();
}
