// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_stored_webhook_dns_temporarily_fails : given.an_observer
{
    Exception _error;
    ObserverRunningState _stateAfterFailure;
    bool _subscribedAfterRetry;
    readonly SocketException _dnsError = new((int)SocketError.TimedOut);

    void Establish() => _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
        .Returns(_ => Task.FromException<WebhookTargetValidation>(_dnsError), _ => Task.FromResult(WebhookTargetValidation.Allowed));

    async Task Because()
    {
        _error = await Catch.Exception(async () => await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero));
        _stateAfterFailure = _stateStorage.State.RunningState;
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero);
        _subscribedAfterRetry = await _observer.IsSubscribed();
    }

    [Fact] void should_propagate_the_retryable_error() => _error.ShouldEqual(_dnsError);
    [Fact] void should_not_quarantine_on_dns_failure() => _stateAfterFailure.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_subscribe_when_retried() => _subscribedAfterRetry.ShouldBeTrue();
    [Fact] void should_not_quarantine_on_retry() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_create_partition_failures() => _failedPartitionsState.Partitions.ShouldBeEmpty();
}
