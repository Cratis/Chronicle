// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_the_definition_changes;

public class and_replay_is_enabled : given.a_client_reducer_subscription_with_replay_on_definition_change
{
    async Task Because()
    {
        await _subscription.SetDefinitionAndSubscribe(_definition);
        await _subscription.ScheduledReplay;
    }

    [Fact] Task should_replay_the_observer_in_every_namespace() => _observer.Received(1).Replay();
}
