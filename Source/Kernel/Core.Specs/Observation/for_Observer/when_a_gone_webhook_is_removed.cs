// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_webhook_is_removed : given.a_gone_retired_webhook
{
    async Task Because()
    {
        // Cleanup already completed in the fixture. Removal must still clear the terminal incident.
        _eventStoreStorage.Webhooks.Has(_webhookDefinition.Identifier).Returns(false);
        await _observer.RunWatchdogAsync();
        await Crash();
        await ReportAlerts();
    }

    [Fact] void should_remove_the_terminal_reason() => _stateStorage.State.WebhookRetirement.ShouldBeNull();
    [Fact] void should_stay_retired() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_preserve_the_partition_cursor() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_preserve_failure_history() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(1);
}
