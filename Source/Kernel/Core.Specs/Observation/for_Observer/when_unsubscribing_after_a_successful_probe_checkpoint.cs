// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_unsubscribing_after_a_successful_probe_checkpoint : given.a_webhook_ready_to_probe
{
    async Task Establish()
    {
        _observerHandledCountsStorage.SetForRecovery(_observerId, "partition", Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>()).Returns(Task.FromException(new InvalidOperationException("Local storage unavailable")));
        await Tick();
        _observerHandledCountsStorage.SetForRecovery(_observerId, "partition", Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>()).Returns(Task.CompletedTask);
    }

    Task Because() => _observer.Unsubscribe();

    [Fact] void should_preserve_the_successful_delivery_progress() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_clear_probe_authority() => _stateStorage.State.WebhookProbe.ShouldBeNull();
    [Fact] void should_not_deliver_again() => _webhook.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
}
