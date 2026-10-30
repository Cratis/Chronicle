// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_succeeds : given.a_webhook_ready_to_probe
{
    Task Because() => Tick();

    [Fact] void should_deliver_the_real_event_once() => _webhook.Received(1).OnNext(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Single().Context.SequenceNumber == (EventSequenceNumber)42UL), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_save_the_next_undelivered_event() => _failedPartitionsState.Partitions.Single().RecoveryCursor.Position.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_reset_the_retry_budget() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(0);
    [Fact] void should_keep_attempt_history() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
    [Fact] void should_leave_observer_quarantine() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_remove_probe_control() => _stateStorage.State.WebhookProbe.ShouldBeNull();
    [Fact] void should_add_one_to_partition_counts() => _observerHandledCountsStorage.Received(1).SetForRecovery(_observerId, "partition", Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(counts => counts[(EventTypeId)"Event1"].Value == 1));
}
