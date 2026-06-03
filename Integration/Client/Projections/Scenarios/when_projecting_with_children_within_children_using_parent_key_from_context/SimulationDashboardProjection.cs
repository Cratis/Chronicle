// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context;

public class SimulationDashboardProjection : IProjectionFor<SimulationDashboard>
{
    public void Define(IProjectionBuilderFor<SimulationDashboard> builder) => builder
        .AutoMap()
        .From<SimulationAdded>()
        .Children(m => m.Configurations, m => m
            .IdentifiedBy(r => r.ConfigurationId)
            .From<SimulationConfigurationAdded>(b => b.UsingKey(e => e.ConfigurationId))
            .From<WeightsSetForSimulationConfiguration>()
            .Children(m => m.Hubs, m => m
                .IdentifiedBy(r => r.HubId)
                .From<HubAddedToSimulationConfiguration>(e => e.UsingKey(e => e.HubId))
                .Children(h => h.Metrics, h => h
                    .IdentifiedBy(r => r.MetricId)
                    .From<MetricAddedToHub>(e => e.UsingKey(e => e.MetricId)))));
}
