// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_deferred_webhook_probe_reactivates : given.a_webhook_probe_with_a_receiver_deferral
{
    async Task Establish()
    {
        await Tick();
        var clock = _observer.ProbeTimeProvider;
        await Crash();
        _observer.ProbeTimeProvider = clock;
        _silo.AddProbe<IWebhookObserverSubscriber>(_ => _webhook);
        _webhook.ClearReceivedCalls();
        await _observer.ReconcileSubscription<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, false);
        _now = _deadline - TimeSpan.FromSeconds(1);
    }

    Task Because() => Tick();

    [Fact] void should_not_deliver_before_the_stored_deadline() => _webhook.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_resample_the_deadline() => _stateStorage.State.WebhookProbe.Due.ShouldEqual(_deadline);
    [Fact] void should_keep_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
