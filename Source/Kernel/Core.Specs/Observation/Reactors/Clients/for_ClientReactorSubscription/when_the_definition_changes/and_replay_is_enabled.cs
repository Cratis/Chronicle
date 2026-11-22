// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ClientReactorSubscription.when_the_definition_changes;

public class and_replay_is_enabled : given.a_client_reactor_subscription_with_replay_on_definition_change
{
    async Task Because()
    {
        await _subscription.SetDefinitionAndSubscribe(_definition);
        await _subscription.ScheduledReplay;
    }

    [Fact] Task should_replay_the_observer_in_every_namespace() => _observerProbe.Received(1).Replay();
    [Fact] Task should_not_recommend_a_replay() => _recommendationsProbe.DidNotReceiveWithAnyArgs().Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(default!, default!);
}
