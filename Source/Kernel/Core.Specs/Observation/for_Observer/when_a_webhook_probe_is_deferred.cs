// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_is_deferred : given.a_webhook_ready_to_probe
{
    DateTimeOffset _deadline;

    void Establish()
    {
        _deadline = _now + TimeSpan.FromHours(3);
        _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>()).Returns(new ObserverSubscriberResult(ObserverSubscriberState.Deferred, EventSequenceNumber.Unavailable, [], "")
        {
            Deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, _now, _deadline, 42UL)
        });
    }

    Task Because() => Tick();

    [Fact] void should_honor_the_receiver_deadline() => _stateStorage.State.WebhookProbe.Due.ShouldEqual(_deadline);
    [Fact] void should_not_add_an_attempt() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
    [Fact] void should_keep_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
