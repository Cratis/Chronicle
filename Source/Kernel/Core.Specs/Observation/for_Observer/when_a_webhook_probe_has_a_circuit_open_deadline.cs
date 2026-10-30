// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_has_a_circuit_open_deadline : given.a_webhook_probe_with_a_receiver_deferral
{
    void Establish()
    {
        _deferral = _deferral with { Reason = PartitionDeferralReason.CircuitOpen };
        _failedPartitionsState.Partitions.Single().Deferral = _deferral;
    }

    Task Because() => Tick();

    [Fact] void should_not_deliver_before_the_deadline() => _webhook.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_persist_the_circuit_deadline() => _stateStorage.State.WebhookProbe.Due.ShouldEqual(_deadline);
    [Fact] void should_keep_the_partition_deferral() => _failedPartitionsState.Partitions.Single().Deferral.ShouldEqual(_deferral);
    [Fact] void should_not_spend_an_attempt() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_keep_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
