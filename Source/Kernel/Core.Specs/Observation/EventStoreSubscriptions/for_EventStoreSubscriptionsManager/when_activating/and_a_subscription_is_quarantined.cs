// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_activating;

public class and_a_subscription_is_quarantined : given.a_manager_with_a_quarantined_subscription
{
    async Task Because() => await _manager.OnActivateAsync(CancellationToken.None);

    [Fact] void should_not_revive_the_observer() => ShouldNotSubscribe(_observer);
    [Fact] void should_schedule_the_reminder() => ShouldScheduleReminder(2);
}
