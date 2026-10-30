// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_an_observer_was_lost_with_a_departed_silo : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _definition = CreateDefinition("the-projection", "the-read-model");
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
        await _grain.Register([_definition]);
        await _silo.TimerRegistry.FireAllAsync();
        _observerGrain.ClearReceivedCalls();
        _observerGrain.IsSubscriptionEstablished().Returns(false);
        SiloChanged(_otherSilo, SiloStatus.Dead);
    }

    async Task Because()
    {
        await _grain.Register([_definition]);
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_check_real_observer_state() => await _observerGrain.Received().IsSubscriptionEstablished();
    [Fact]
    async Task should_reconcile_the_subscription() =>
        await _observerGrain.Received().ReconcileSubscription<IProjectionObserverSubscriber>(
            ObserverType.Projection,
            Arg.Any<IEnumerable<Concepts.Events.EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<bool>(),
            Arg.Any<EventGenerationDelivery>());
}
