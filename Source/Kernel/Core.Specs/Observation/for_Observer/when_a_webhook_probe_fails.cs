// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_fails : given.a_webhook_ready_to_probe
{
    void Establish() => _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>()).Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "503"));

    async Task Because()
    {
        await Tick();
        await Tick();
    }

    [Fact] void should_keep_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_add_attempts() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
    [Fact] void should_not_change_the_retry_budget() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_back_off_independently() => _stateStorage.State.WebhookProbe.Failures.ShouldEqual(1);
    [Fact] void should_not_deliver_again_on_an_early_tick() => _webhook.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_sample_the_deadline_once() => _stateStorage.State.WebhookProbe.Due.ShouldEqual(_now + TimeSpan.FromSeconds(120));
}
