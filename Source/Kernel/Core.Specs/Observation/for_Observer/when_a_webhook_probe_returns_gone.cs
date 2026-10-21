// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_returns_gone : given.a_webhook_ready_to_probe
{
    long _revision;

    void Establish()
    {
        _revision = _stateStorage.State.AlertRevision;
        _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>()).Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "410") with { FailurePolicy = WebhookDeliveryFailurePolicy.ParkPartition });
    }

    async Task Because()
    {
        await Tick();
        _now = _stateStorage.State.WebhookProbe.Due;
        await Tick();
    }

    [Fact] void should_advance_the_alert_revision() => _stateStorage.State.AlertRevision.ShouldBeGreaterThan(_revision);
    [Fact] void should_park_the_episode() => _failedPartitionsState.Partitions.Single().WebhookDisposition.ShouldEqual(WebhookFailureDisposition.Gone);
    [Fact] void should_not_probe_the_gone_episode_again() => _webhook.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_keep_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
