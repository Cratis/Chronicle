// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_probe_is_disabled : given.a_webhook_observer
{
    void Establish() => _silo.ServiceProvider.GetRequiredService<IOptionsMonitor<ChronicleOptions>>().CurrentValue.Returns(new ChronicleOptions { Webhooks = new() { Probe = new() { Enabled = false } } });

    Task Because() => _observer.PartitionFailed("partition", 42UL, ["403"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.PauseObserver);

    [Fact] void should_retain_eligibility_without_scheduling_delivery() => _stateStorage.State.WebhookProbe.ShouldNotBeNull();
    [Fact] void should_keep_the_observer_paused() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_register_a_probe_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), "webhook-quarantine-probe", It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never());
}
