// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

/// <summary>
/// The projection engine compiles a projection for a namespace when an observer of that namespace first asks for it, so
/// subscribing in a namespace sets nothing up in the engine of every silo ahead of time.
/// </summary>
public class and_the_observer_is_not_subscribed : given.a_projections_manager_grain
{
    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true }];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain.IsSubscriptionEstablished().Returns(false, true);
    }

    async Task Because() => await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);

    [Fact] async Task should_subscribe_the_observer() => await _observerGrain.Received(1).ReconcileSubscription<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<EventTypeSet>(), Arg.Any<SiloAddress>(), true);
    [Fact] async Task should_not_set_up_the_namespace_in_the_engine_of_every_silo() => await _projectionsServiceClient.DidNotReceiveWithAnyArgs().NamespaceAdded(default!, default!);
}
