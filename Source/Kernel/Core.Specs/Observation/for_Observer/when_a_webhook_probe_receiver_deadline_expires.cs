// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_receiver_deadline_expires : given.a_webhook_probe_with_a_receiver_deferral
{
    async Task Because()
    {
        await Tick();
        _now = _deadline;
        await Tick();
    }

    [Fact] void should_deliver_once_at_the_deadline() => _webhook.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_advance_past_the_delivered_event() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_clear_the_consumed_deferral() => _failedPartitionsState.Partitions.Single().Deferral.ShouldBeNull();
}
