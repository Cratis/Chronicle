// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_receiver_defers_during_webhook_probe_preparation : given.a_webhook_ready_to_probe
{
    DateTimeOffset _deadline;

    void Establish()
    {
        _deadline = _now + TimeSpan.FromHours(3);
        _observerHandledCountsStorage.GetFor(_observerId, "partition").Returns(_ =>
        {
            _failedPartitionsState.Partitions.Single().Deferral = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, _now, _deadline, 42UL);
            return new Dictionary<EventTypeId, EventCount>();
        });
    }

    Task Because() => Tick();

    [Fact] void should_recheck_before_delivery() => _webhook.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_persist_the_new_deadline() => _stateStorage.State.WebhookProbe.Due.ShouldEqual(_deadline);
    [Fact] void should_keep_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
