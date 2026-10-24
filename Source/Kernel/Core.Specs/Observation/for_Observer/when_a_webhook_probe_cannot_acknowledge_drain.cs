// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_cannot_acknowledge_drain : given.a_webhook_ready_to_probe
{
    void Establish() => _webhook.Drain().Returns(Task.FromException(new InvalidOperationException("No drain acknowledgement")));

    async Task Because()
    {
        await Tick();
        _now = _stateStorage.State.WebhookProbe.Due;
        await Tick();
    }

    [Fact] void should_never_start_another_http_delivery() => _webhook.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_keep_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_cursor() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)42UL);
}
