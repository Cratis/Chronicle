// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_local_webhook_probe_recovery_fails : given.a_webhook_ready_to_probe
{
    void Establish() => _observerHandledCountsStorage.SetForRecovery(_observerId, "partition", Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>()).Returns(Task.FromException(new InvalidOperationException("Local storage unavailable")));

    Task Because() => Tick();

    [Fact] void should_checkpoint_success_before_local_recovery() => _stateStorage.State.WebhookProbe.ResumePending.ShouldBeTrue();
    [Fact] void should_record_the_next_undelivered_event() => _stateStorage.State.WebhookProbe.ResumePosition.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_remain_quarantined() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_spend_an_attempt() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(1);
}
