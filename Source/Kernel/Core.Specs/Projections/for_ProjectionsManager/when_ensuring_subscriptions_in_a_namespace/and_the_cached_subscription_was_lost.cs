// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_cached_subscription_was_lost : given.a_projections_manager_grain
{
    bool _subscribed = true;

    async Task Establish()
    {
        var definition = CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true };
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);

        // Drain activation work before registration; a namespace-local trigger repairs later loss.
        await _silo.TimerRegistry.FireAllAsync();
        await _grain.Register([definition]);
        await _observerGrain.Received(1).SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: true);
        _observerGrain.IsSubscribed().Returns(_ => _subscribed);
        _observerGrain.SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: false)
            .Returns(_ =>
            {
                _subscribed = true;
                return Task.CompletedTask;
            });
        _observerGrain.ClearReceivedCalls();
        _subscribed = false;
    }

    Task Because() => _grain.EnsureSubscribedIn(Concepts.EventStoreNamespaceName.Default);

    [Fact] async Task should_repair_the_subscription_despite_the_managers_successful_subscription_cache() =>
        await _observerGrain.Received(1).SubscribeToAllEvents<IProjectionObserverSubscriber>(
            ObserverType.Projection,
            Arg.Any<SiloAddress>(),
            reactivateRetired: false);
}
