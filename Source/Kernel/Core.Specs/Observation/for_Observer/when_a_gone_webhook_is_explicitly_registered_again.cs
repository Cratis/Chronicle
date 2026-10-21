// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_webhook_is_explicitly_registered_again : given.a_gone_retired_webhook
{
    async Task Because() => await RegisterAgain();

    [Fact] void should_clear_the_terminal_reason() => _stateStorage.State.WebhookRetirement.ShouldBeNull();
    [Fact] void should_activate_the_source() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
    [Fact] void should_start_a_new_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldNotEqual(_goneAttempt.LifecycleId);
    [Fact] void should_keep_the_failed_event() => _failedPartitionsState.Partitions.Single().RecoveryCursor.Position.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_reset_the_recovery_budget_once() => _failedPartitionsState.Partitions.Single().WebhookRecoveryId.ShouldEqual(_registrationId);
    [Fact] void should_keep_the_attempt_history() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
}
