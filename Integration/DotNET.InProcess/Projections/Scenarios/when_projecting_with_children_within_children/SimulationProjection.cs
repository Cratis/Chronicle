// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.ReadModels;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children;

public class SimulationProjection : IProjectionFor<Simulation>
{
    public void Define(IProjectionBuilderFor<Simulation> builder) => builder
        .AutoMap()
        .From<SimulationAdded>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(_ => _.Configurations, _ => _
            .IdentifiedBy(c => c.ConfigurationId)
            .From<ConfigurationAddedToSimulation>(b => b
                .UsingParentKey(e => e.SimulationId)
                .UsingKey(e => e.ConfigurationId))
            .Children(c => c.Hubs, h => h
                .IdentifiedBy(hub => hub.HubId)
                .From<HubAddedToConfiguration>(b => b
                    .UsingParentKey(e => e.ConfigurationId)
                    .UsingKey(e => e.HubId))));
}
