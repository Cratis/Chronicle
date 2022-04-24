// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_reminded;

public class and_no_failed_partitions : given.an_observer_with_event_types_and_reminder
{
    async Task Because() => await observer.ReceiveReminder(Observer.RecoverReminder, new TickStatus());

    [Fact] void should_unregister_reminder() => reminder_registry.Verify(_ => _.UnregisterReminder(reminder.Object), Once());
}
