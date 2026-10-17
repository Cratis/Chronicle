// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_observer_is_already_subscribed : given.a_projections_manager_grain
{
    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true }];
        _observerGrain.IsSubscriptionEstablished().Returns(true);
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
        await _observerGrain.Received(1).IsSubscriptionEstablished();
        _observerGrain.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _grain.Register([]);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_cache_the_pair_so_registration_skips_it() => await _observerGrain.DidNotReceive().IsSubscriptionEstablished();
    [Fact] async Task should_not_set_the_projection_definition_again() => await _projectionGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);
    [Fact] async Task should_not_resubscribe_the_observer() => await _observerGrain.DidNotReceive().SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: Arg.Any<bool>());
    [Fact] async Task should_not_repeat_engine_namespace_setup() => await _projectionsServiceClient.DidNotReceiveWithAnyArgs().NamespaceAdded(default!, default!);
}
