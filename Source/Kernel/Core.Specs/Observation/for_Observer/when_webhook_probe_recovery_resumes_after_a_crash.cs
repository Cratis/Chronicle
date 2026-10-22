// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_webhook_probe_recovery_resumes_after_a_crash : given.a_webhook_ready_to_probe
{
    async Task Establish()
    {
        _observerHandledCountsStorage.SetForRecovery(_observerId, "partition", Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>()).Returns(Task.FromException(new InvalidOperationException("Local storage unavailable")));
        await Tick();
        var clock = _observer.ProbeTimeProvider;
        await Crash();
        _observer.ProbeTimeProvider = clock;
        _silo.AddProbe<IWebhookObserverSubscriber>(_ => _webhook);
        _observerHandledCountsStorage.SetForRecovery(_observerId, "partition", Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>()).Returns(Task.CompletedTask);
        _webhook.ClearReceivedCalls();
        await _observer.ReconcileSubscription<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, false);
    }

    Task Because() => Tick();

    [Fact] void should_not_send_the_successful_probe_again() => _webhook.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_resume_at_the_next_undelivered_event() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_reset_the_eligible_budget_once() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(0);
    [Fact] void should_remove_the_finished_checkpoint() => _stateStorage.State.WebhookProbe.ShouldBeNull();
}
