// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_a_silo_leaves_the_cluster;

public class and_a_silo_joins : given.a_projections_manager_grain
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
        var joined = SiloAddress.New(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 11113), 1);
        SiloChanged(joined, SiloStatus.Active);
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_not_check_real_observer_state() => await _observerGrain.DidNotReceive().IsSubscriptionEstablished();
}
