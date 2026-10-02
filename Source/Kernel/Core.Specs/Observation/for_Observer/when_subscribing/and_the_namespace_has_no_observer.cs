// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_the_namespace_has_no_observer : given.an_absent_observer
{
    async Task Establish() => await Crash();

    async Task Because() => await _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);

    [Fact] void should_persist_a_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldNotEqual(Guid.Empty);
    [Fact] void should_register_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.AlertReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)), Times.Once);
    [Fact] async Task should_subscribe() => (await _observer.IsSubscribed()).ShouldBeTrue();
}
