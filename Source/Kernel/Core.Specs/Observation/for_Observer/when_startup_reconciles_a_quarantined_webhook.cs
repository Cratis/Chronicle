// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_startup_reconciles_a_quarantined_webhook : given.a_webhook_ready_to_probe
{
    Guid _probe;

    void Establish() => _probe = _stateStorage.State.WebhookProbe.Id;

    Task Because() => _observer.ReconcileSubscription<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, false);

    [Fact] void should_preserve_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_same_probe_generation() => _stateStorage.State.WebhookProbe.Id.ShouldEqual(_probe);
    [Fact] void should_not_reset_the_retry_budget() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(1);
}
