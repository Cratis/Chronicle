// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_sees_legacy_policy : given.a_webhook_ready_to_probe
{
    void Establish() => _failedPartitionsState.Partitions.Single().WebhookDisposition = null;

    Task Because() => Tick();

    [Fact] void should_not_infer_eligibility_from_old_failure_text() => _webhook.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_remain_quarantined() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
