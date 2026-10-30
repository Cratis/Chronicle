// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_watchdog_enters_quarantine : given.a_webhook_observer
{
    Task Because() => _observer.TransitionTo<QuarantinedObserver>();

    [Fact] void should_not_infer_receiver_failure_from_the_running_state() => _stateStorage.State.WebhookProbe.ShouldBeNull();
    [Fact] void should_not_register_a_health_check() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), "webhook-quarantine-probe", It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never());
}
