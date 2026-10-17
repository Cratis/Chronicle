// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_all_namespace_subscriptions_are_cached : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _definition = CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true };
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _namespaces.GetAll().Returns([EventStoreNamespaceName.Default, new EventStoreNamespaceName("tenant-a"), new EventStoreNamespaceName("tenant-b")]);
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
        await _grain.Register([_definition]);
        await _observerGrain.Received(3).SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: true);
        await _silo.TimerRegistry.FireAllAsync();
        _observerGrain.ClearReceivedCalls();
        _projectionGrain.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _grain.Register([_definition]);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_not_check_real_observer_state_in_each_namespace() => await _observerGrain.DidNotReceive().IsSubscribed();
    [Fact] async Task should_not_reset_the_projection_definition() => await _projectionGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);
    [Fact] async Task should_not_subscribe_again() => await _observerGrain.DidNotReceive().SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: Arg.Any<bool>());
}
